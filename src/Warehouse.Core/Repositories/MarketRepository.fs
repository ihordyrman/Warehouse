namespace Warehouse.Core.Repositories

open System
open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Infrastructure
open Warehouse.Core.Infrastructure.Entities
open Warehouse.Core.Shared
open Warehouse.Core.Shared.Errors

module MarketRepository =
    open Errors
    open Mappers

    type MarketWithCredentials = { Market: Market; Credentials: MarketCredentials option }

    type CreateMarketRequest =
        { Type: MarketType; ApiKey: string; SecretKey: string; Passphrase: string option; IsSandbox: bool }

    type UpdateCredentialsRequest =
        {
            ApiKey: string option
            SecretKey: string option
            Passphrase: string option
            IsSandbox: bool option
        }

    type T =
        {
            GetById: int -> CancellationToken -> Task<Result<MarketWithCredentials, ServiceError>>
            GetByType: MarketType -> CancellationToken -> Task<Result<MarketWithCredentials option, ServiceError>>
            GetAll: CancellationToken -> Task<Result<MarketWithCredentials list, ServiceError>>
            Create: CreateMarketRequest -> CancellationToken -> Task<Result<Market, ServiceError>>
            UpdateCredentials: int -> UpdateCredentialsRequest -> CancellationToken -> Task<Result<unit, ServiceError>>
            Delete: int -> CancellationToken -> Task<Result<unit, ServiceError>>
            Count: CancellationToken -> Task<Result<int, ServiceError>>
            Exists: MarketType -> CancellationToken -> Task<Result<bool, ServiceError>>
        }

    let private getById (scopeFactory: IServiceScopeFactory) (logger: ILogger) (id: int) (_: CancellationToken) =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! marketResults =
                    db.QueryAsync<MarketEntity>("SELECT * FROM markets WHERE id = @Id LIMIT 1", {| Id = id |})

                match marketResults |> Seq.tryHead with
                | None ->
                    logger.LogWarning("Market {Id} not found", id)
                    return Result.Error(NotFound $"Market with id {id}")
                | Some marketEntity ->
                    let! credResults =
                        db.QueryAsync<MarketCredentialsEntity>(
                            "SELECT * FROM market_credentials WHERE market_id = @MarketId LIMIT 1",
                            {| MarketId = id |}
                        )

                    let credentials = credResults |> Seq.tryHead |> Option.map (fun c -> toMarketCredentials c None)

                    let market = toMarket marketEntity credentials
                    logger.LogDebug("Retrieved market {Id}", id)
                    return Ok { Market = market; Credentials = credentials }
            with ex ->
                logger.LogError(ex, "Failed to get market {Id}", id)
                return Result.Error(Unexpected ex)
        }

    let private getByType
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (marketType: MarketType)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! marketResults =
                    db.QueryAsync<MarketEntity>(
                        "SELECT * FROM markets WHERE type = @Type LIMIT 1",
                        {| Type = int marketType |}
                    )

                match marketResults |> Seq.tryHead with
                | None ->
                    logger.LogDebug("Market type {MarketType} not found", marketType)
                    return Ok None
                | Some marketEntity ->
                    let! credResults =
                        db.QueryAsync<MarketCredentialsEntity>(
                            "SELECT * FROM market_credentials WHERE market_id = @MarketId LIMIT 1",
                            {| MarketId = marketEntity.Id |}
                        )

                    let credentials = credResults |> Seq.tryHead |> Option.map (fun c -> toMarketCredentials c None)

                    let market = toMarket marketEntity credentials
                    logger.LogDebug("Retrieved market type {MarketType}", marketType)
                    return Ok(Some { Market = market; Credentials = credentials })
            with ex ->
                logger.LogError(ex, "Failed to get market type {MarketType}", marketType)
                return Result.Error(Unexpected ex)
        }

    let private getAll (scopeFactory: IServiceScopeFactory) (logger: ILogger) (_: CancellationToken) =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! results =
                    db.QueryAsync<MarketEntity, MarketCredentialsEntity, MarketEntity * MarketCredentialsEntity>(
                        "SELECT m.*, mc.*
                         FROM markets m
                         LEFT JOIN market_credentials mc ON m.id = mc.market_id
                         ORDER BY m.id",
                        (fun market creds -> (market, creds)),
                        splitOn = "id"
                    )

                let markets =
                    results
                    |> Seq.map (fun (marketEntity, credsEntity) ->
                        let credentials = credsEntity |> fun c -> toMarketCredentials c None |> Some
                        let market = toMarket marketEntity credentials
                        { Market = market; Credentials = credentials }
                    )
                    |> Seq.toList

                logger.LogDebug("Retrieved {Count} markets", markets.Length)
                return Ok markets
            with ex ->
                logger.LogError(ex, "Failed to get all markets")
                return Result.Error(Unexpected ex)
        }

    let private createMarket
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (request: CreateMarketRequest)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! existingCount =
                    db.QuerySingleAsync<int>(
                        "SELECT COUNT(1) FROM markets WHERE type = @Type",
                        {| Type = int request.Type |}
                    )

                if existingCount > 0 then
                    logger.LogWarning("Market type {MarketType} already exists", request.Type)
                    return Result.Error(ApiError($"Market {request.Type} already exists", Some 409))
                else
                    let now = DateTime.UtcNow

                    let! marketId =
                        db.QuerySingleAsync<int>(
                            "INSERT INTO markets (type, created_at, updated_at)
                             VALUES (@Type, @CreatedAt, @UpdatedAt)
                             RETURNING id",
                            {| Type = int request.Type; CreatedAt = now; UpdatedAt = now |}
                        )

                    let! _ =
                        db.ExecuteAsync(
                            "INSERT INTO market_credentials (market_id, api_key, secret_key, passphrase, is_sandbox, created_at, updated_at)
                             VALUES (@MarketId, @ApiKey, @SecretKey, @Passphrase, @IsSandbox, @CreatedAt, @UpdatedAt)",
                            {|
                                MarketId = marketId
                                ApiKey = request.ApiKey
                                SecretKey = request.SecretKey
                                Passphrase = request.Passphrase |> Option.defaultValue ""
                                IsSandbox = request.IsSandbox
                                CreatedAt = now
                                UpdatedAt = now
                            |}
                        )

                    logger.LogInformation("Created market {Id} of type {MarketType}", marketId, request.Type)

                    let market: Market =
                        {
                            Id = marketId
                            Type = request.Type
                            Credentials = Unchecked.defaultof<MarketCredentials>
                            CreatedAt = now
                            UpdatedAt = now
                        }

                    return Ok market
            with ex ->
                logger.LogError(ex, "Failed to create market of type {MarketType}", request.Type)
                return Result.Error(Unexpected ex)
        }

    let private updateCredentials
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (marketId: int)
        (request: UpdateCredentialsRequest)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! marketExists =
                    db.QuerySingleAsync<int>("SELECT COUNT(1) FROM markets WHERE id = @Id", {| Id = marketId |})

                if marketExists = 0 then
                    logger.LogWarning("Market {Id} not found for credentials update", marketId)
                    return Result.Error(NotFound $"Market with id {marketId}")
                else
                    let now = DateTime.UtcNow
                    let updates = ResizeArray<string>()
                    let parameters = DynamicParameters()
                    parameters.Add("MarketId", marketId)
                    parameters.Add("Now", now)

                    match request.ApiKey with
                    | Some apiKey when not (String.IsNullOrWhiteSpace apiKey) ->
                        updates.Add("api_key = @ApiKey")
                        parameters.Add("ApiKey", apiKey.Trim())
                    | _ -> ()

                    match request.SecretKey with
                    | Some secretKey when not (String.IsNullOrWhiteSpace secretKey) ->
                        updates.Add("secret_key = @SecretKey")
                        parameters.Add("SecretKey", secretKey.Trim())
                    | _ -> ()

                    match request.Passphrase with
                    | Some passphrase ->
                        updates.Add("passphrase = @Passphrase")
                        parameters.Add("Passphrase", passphrase.Trim())
                    | _ -> ()

                    match request.IsSandbox with
                    | Some isSandbox ->
                        updates.Add("is_sandbox = @IsSandbox")
                        parameters.Add("IsSandbox", isSandbox)
                    | _ -> ()

                    if updates.Count > 0 then
                        updates.Add("updated_at = @Now")
                        let setClause = String.Join(", ", updates)
                        let updateSql = $"UPDATE market_credentials SET {setClause} WHERE market_id = @MarketId"
                        let! _ = db.ExecuteAsync(updateSql, parameters)
                        ()

                    let! _ =
                        db.ExecuteAsync(
                            "UPDATE markets SET updated_at = @Now WHERE id = @Id",
                            {| Now = now; Id = marketId |}
                        )

                    logger.LogInformation("Updated credentials for market {Id}", marketId)
                    return Ok()
            with ex ->
                logger.LogError(ex, "Failed to update credentials for market {Id}", marketId)
                return Result.Error(Unexpected ex)
        }

    let private deleteMarket
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (marketId: int)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! _ =
                    db.ExecuteAsync(
                        "DELETE FROM market_credentials WHERE market_id = @MarketId",
                        {| MarketId = marketId |}
                    )

                let! rowsAffected = db.ExecuteAsync("DELETE FROM markets WHERE id = @Id", {| Id = marketId |})

                if rowsAffected > 0 then
                    logger.LogInformation("Deleted market {Id}", marketId)
                    return Ok()
                else
                    logger.LogWarning("Market {Id} not found for deletion", marketId)
                    return Result.Error(NotFound $"Market with id {marketId}")
            with ex ->
                logger.LogError(ex, "Failed to delete market {Id}", marketId)
                return Result.Error(Unexpected ex)
        }

    let private count (scopeFactory: IServiceScopeFactory) (logger: ILogger) (_: CancellationToken) =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! result = db.QuerySingleAsync<int>("SELECT COUNT(1) FROM markets")

                logger.LogDebug("Market count: {Count}", result)
                return Ok result
            with ex ->
                logger.LogError(ex, "Failed to count markets")
                return Result.Error(Unexpected ex)
        }

    let private exists
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (marketType: MarketType)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! result =
                    db.QuerySingleAsync<int>(
                        "SELECT COUNT(1) FROM markets WHERE type = @Type",
                        {| Type = int marketType |}
                    )

                logger.LogDebug("Market type {MarketType} exists: {Exists}", marketType, result > 0)
                return Ok(result > 0)
            with ex ->
                logger.LogError(ex, "Failed to check if market type {MarketType} exists", marketType)
                return Result.Error(Unexpected ex)
        }

    let create (scopeFactory: IServiceScopeFactory) : T =
        let loggerFactory = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILoggerFactory>()
        let logger = loggerFactory.CreateLogger("MarketRepository")

        {
            GetById = getById scopeFactory logger
            GetByType = getByType scopeFactory logger
            GetAll = getAll scopeFactory logger
            Create = createMarket scopeFactory logger
            UpdateCredentials = updateCredentials scopeFactory logger
            Delete = deleteMarket scopeFactory logger
            Count = count scopeFactory logger
            Exists = exists scopeFactory logger
        }
