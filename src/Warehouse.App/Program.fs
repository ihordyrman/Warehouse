open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.HttpLogging
open Microsoft.Extensions.DependencyInjection
open Serilog
open System
open Warehouse.App.Pages
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
            get "/" Index.get

            get "/balance/total" Balance.Handler.total
            mapGet "/balance/{marketType:int}" _.GetInt("marketType") Balance.Handler.market

            get "/markets/count" Markets.Handler.count
            get "/markets/grid" Markets.Handler.grid

            get "/pipelines/count" Pipeline.Handler.count
            get "/pipelines/grid" Pipeline.Handler.grid
            get "/pipelines/table" Pipeline.Handler.table

            get "/pipelines/modal" CreatePipeline.Handler.modal
            get "/pipelines/modal/close" CreatePipeline.Handler.closeModal
            post "/pipelines/create" CreatePipeline.Handler.create

            get "/system-status" SystemStatus.Handler.status

        ]
    )
    .Run(Response.ofPlainText "Not found")
