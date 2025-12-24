namespace Warehouse.Core.Pipelines

open System
open Warehouse.Core.Pipelines

module PipelineBuilder =
    open Steps

    type PipelineStepConfig = { StepTypeKey: string; Order: int; IsEnabled: bool; Parameters: Map<string, string> }

    type BuildError = { StepKey: string; Errors: Parameters.ValidationError list }

    let buildSteps
        (registry: StepRegistry.T<'ctx>)
        (services: IServiceProvider)
        (stepConfigs: PipelineStepConfig list)
        : Result<Step<'ctx> list, BuildError list>
        =

        let result =
            stepConfigs
            |> List.filter _.IsEnabled
            |> List.sortBy _.Order
            |> List.choose (fun config ->
                registry
                |> StepRegistry.tryFind config.StepTypeKey
                |> Option.map (fun def ->
                    match Parameters.validate def.ParameterSchema config.Parameters with
                    | Ok validParams -> Ok(def.Create validParams services)
                    | Error errs -> Error { StepKey = config.StepTypeKey; Errors = errs }
                )
            )

        let errors =
            result
            |> List.choose (
                function
                | Error e -> Some e
                | _ -> None
            )

        if not (List.isEmpty errors) then
            Error errors
        else
            Ok(
                result
                |> List.choose (
                    function
                    | Ok step -> Some step
                    | _ -> None
                )
            )
