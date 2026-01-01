open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.HttpLogging
open Microsoft.Extensions.DependencyInjection
open Serilog
open System
open Warehouse.App
open Warehouse.Core

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

CoreServices.register webapp.Services webapp.Configuration

webapp.Services.AddHttpLogging(
    Action<HttpLoggingOptions>(fun options -> options.LoggingFields <- HttpLoggingFields.All)
)
|> ignore

let app = webapp.Build()

app.UseHttpLogging() |> ignore
app.UseHttpsRedirection() |> ignore
app.UseRouting() |> ignore
app.UseDefaultFiles().UseStaticFiles() |> ignore

app
    .UseFalco(
        [
            get "/" Views.Index.get
            get "/balance/total" Handlers.Balances.total
            get "/create-pipeline" Views.CreatePipeline.get
            get "/markets/count" Handlers.Markets.count
            get "/markets/grid" Handlers.Markets.grid
            get "/pipelines/count" Handlers.Pipelines.count
            get "/pipelines/grid" Handlers.Pipelines.grid
            get "/system-status" Handlers.System.status
            post "/pipelines/create" Handlers.CreatePipeline.create
            mapGet "/balance/{marketType:int}" _.GetInt("marketType") Handlers.Balances.market
        ]
    )
    .Run(Response.ofPlainText "Not found")
