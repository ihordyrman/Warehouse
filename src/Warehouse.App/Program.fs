open Falco
open Falco.Routing
open FluentMigrator.Runner
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.HttpLogging
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Npgsql
open Serilog
open System
open System.Data
open Warehouse.App.Functional
open Warehouse.Core.Infrastructure.Common
open Warehouse.Core.Infrastructure.Persistence.Migrations

let ensureDbReadiness (serviceProvider: IServiceProvider) =
    task {
        use scope = serviceProvider.CreateScope()
        let connection = scope.ServiceProvider.GetRequiredService<IDbConnection>()
        let configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>()
        let migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>()
        migrationRunner.MigrateUp()
        do! Seeding.ensureCredentialsPopulated configuration connection
    }

let webapp = WebApplication.CreateBuilder()

webapp.Host.UseSerilog(fun context services configuration ->
    configuration.ReadFrom
        .Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
    |> ignore
)
|> ignore

// DependencyInjection.AddOkxSupport(webapp.Services, webapp.Configuration) |> ignore

webapp.Services.Configure<DatabaseSettings>(webapp.Configuration.GetSection(DatabaseSettings.SectionName))
|> ignore

webapp.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(fun x ->
        x
            .AddPostgres()
            .WithGlobalConnectionString(fun x ->
                let settings = x.GetRequiredService<IOptions<DatabaseSettings>>().Value
                settings.ConnectionString
            )
            .ScanIn(typeof<InitialMigration>.Assembly)
            .For.Migrations()
        |> ignore
    )
|> ignore

webapp.Services.AddTransient<IDbConnection>(fun x ->
    let settings = x.GetRequiredService<IOptions<DatabaseSettings>>().Value
    new NpgsqlConnection(settings.ConnectionString)
)
|> ignore

webapp.Services.AddHttpLogging(
    Action<HttpLoggingOptions>(fun options -> options.LoggingFields <- HttpLoggingFields.All)
)
|> ignore

let app = webapp.Build()

ensureDbReadiness app.Services |> Async.AwaitTask |> Async.RunSynchronously

app.UseHttpLogging() |> ignore
app.UseHttpsRedirection() |> ignore
app.UseRouting() |> ignore
app.UseDefaultFiles().UseStaticFiles() |> ignore

app
    .UseFalco(
        [
            get "/" Views.Index.get
            get "/system-status" Handlers.System.status
            get "/accounts/count" Handlers.Accounts.count
            get "/pipelines/count" Handlers.Pipelines.count
            get "/balance/total" Handlers.Balances.total
        ]
    )
    .Run(Response.ofPlainText "Not found")
