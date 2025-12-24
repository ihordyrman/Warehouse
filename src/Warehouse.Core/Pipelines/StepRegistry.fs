namespace Warehouse.Core.Pipelines

module StepRegistry =
    open Steps

    type T<'ctx> = private { Definitions: Map<string, StepDefinition<'ctx>> }

    let empty<'ctx> : T<'ctx> = { Definitions = Map.empty }

    let register (definition: StepDefinition<'ctx>) (registry: T<'ctx>) : T<'ctx> =
        { Definitions = Map.add definition.Key definition registry.Definitions }

    let tryFind (key: string) (registry: T<'ctx>) : StepDefinition<'ctx> option = Map.tryFind key registry.Definitions

    let all (registry: T<'ctx>) : StepDefinition<'ctx> list = registry.Definitions |> Map.values |> List.ofSeq

    let create (definitions: StepDefinition<'ctx> list) : T<'ctx> =
        definitions |> List.map (fun d -> d.Key, d) |> Map.ofList |> (fun m -> { Definitions = m })
