// For more information see https://aka.ms/fsharp-console-apps

open System.IO
open Dapper
open Microsoft.Extensions.Configuration
open Npgsql

type Pipeline = { Id: int; Name: string; Symbol: string }

let builder =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets("90b1b531-55ba-4a18-a28e-d7ed7e5f201d")
        .AddJsonFile("appsettings.json", true)

let configuration = builder.Build()
let connectionString = configuration.GetSection("Database:ConnectionString").Value

DefaultTypeMap.MatchNamesWithUnderscores <- true
let db = new NpgsqlConnection(connectionString) :> System.Data.IDbConnection

let pipelines =
    db.QueryAsync<Pipeline>("SELECT id, name, symbol FROM pipeline_configurations")
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> Seq.toList


printfn $"Loaded %d{List.length pipelines} pipelines"
