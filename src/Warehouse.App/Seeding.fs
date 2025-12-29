module Warehouse.App.Seeding

open System
open System.Data
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.Configuration
open Warehouse.Core.Domain

let ensureCredentialsPopulated (configuration: IConfiguration) (connection: IDbConnection) : Task<unit> =
    task {
        let! credentialCount = connection.QuerySingleAsync<int>("SELECT count(*) FROM market_credentials")

        if credentialCount > 0 then
            return ()
        else
            let section = "OkxAuthConfiguration"
            let apiKey = configuration[$"{section}:ApiKey"]
            let passPhrase = configuration[$"{section}:Passphrase"]
            let secretKey = configuration[$"{section}:SecretKey"]

            match (apiKey, passPhrase, secretKey) with
            | null, _, _
            | _, null, _
            | _, _, null -> failwith "Missing OKX configuration"
            | _ -> ()

            let! marketId =
                task {
                    let! existingMarketId =
                        connection.QuerySingleOrDefaultAsync<Nullable<int>>(
                            "SELECT id FROM markets WHERE type = @Type",
                            {| Type = (int) MarketType.Okx |}
                        )

                    match existingMarketId with
                    | _ when existingMarketId.HasValue -> return existingMarketId.Value
                    | _ ->
                        return!
                            connection.QuerySingleAsync<int>(
                                "INSERT INTO markets (type, created_at, updated_at)
                                 VALUES (@Type, now(), now()) RETURNING id",
                                {| Type = (int) MarketType.Okx |}
                            )
                }

            let! _ =
                connection.ExecuteAsync(
                    """
                    INSERT INTO market_credentials (api_key, passphrase, secret_key, market_id, is_sandbox, created_at, updated_at)
                    VALUES (@ApiKey, @Passphrase, @SecretKey, @MarketId, @IsSandbox, now(), now())
                    """,
                    {|
                        ApiKey = apiKey
                        Passphrase = passPhrase
                        SecretKey = secretKey
                        MarketId = marketId
                        IsSandbox = true
                    |}
                )

            return ()
    }
