namespace Warehouse.Core.Pipelines.Steps

open System

[<AttributeUsage(AttributeTargets.Class, Inherited = false)>]
type StepDefinitionAttribute(key: string) =
    inherit Attribute()

    member _.Key = key
