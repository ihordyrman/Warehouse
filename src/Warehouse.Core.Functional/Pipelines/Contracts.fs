namespace Warehouse.Core.Functional.Pipelines.Contracts

open System
open System.Collections.Generic
open System.Globalization
open System.Threading
open System.Threading.Tasks
open Warehouse.Core.Functional.Pipelines.Domain

type PipelineStepType =
    | Validation = 0
    | RiskManagement = 1
    | SignalGeneration = 2
    | Execution = 3
    | Monitoring = 4

type PipelineStepResult =
    { ShouldContinue: bool
      IsSuccess: bool
      Message: string
      Data: Map<string, obj> }

    static member Continue(message: string) = { ShouldContinue = true; IsSuccess = true; Message = message; Data = Map.empty }
    static member Continue() = { ShouldContinue = true; IsSuccess = true; Message = null; Data = Map.empty }
    static member Stop(message: string) = { ShouldContinue = false; IsSuccess = true; Message = message; Data = Map.empty }
    static member Stop() = { ShouldContinue = false; IsSuccess = true; Message = null; Data = Map.empty }
    static member Error(message: string) = { ShouldContinue = false; IsSuccess = false; Message = message; Data = Map.empty }

type ParameterType =
    | String = 0
    | Integer = 1
    | Decimal = 2
    | Boolean = 3
    | TimeSpan = 4
    | Select = 5

[<CLIMutable>]
type SelectOption = { Label: string; Value: string }

[<CLIMutable>]
type ParameterDefinition =
    { Key: string
      DisplayName: string
      Description: string
      Type: ParameterType
      IsRequired: bool
      DefaultValue: string
      Min: Nullable<decimal>
      Max: Nullable<decimal>
      Options: IReadOnlyList<SelectOption>
      Group: string
      Order: int }

type ValidationError = { Key: string; Message: string }

type ValidationResult = { IsValid: bool; Errors: IReadOnlyList<ValidationError> }

type ParameterBag(values: IReadOnlyDictionary<string, string>) =
    member this.GetString(key: string, defaultValue: string) =
        match values.TryGetValue(key) with
        | true, v -> v
        | _ -> defaultValue

    member this.GetString(key: string) = this.GetString(key, "")

    member this.GetInteger(key: string, defaultValue: int) =
        match values.TryGetValue(key) with
        | true, v ->
            match Int32.TryParse(v, NumberStyles.Any, NumberFormatInfo.InvariantInfo) with
            | true, res -> res
            | _ -> defaultValue
        | _ -> defaultValue

    member this.GetDecimal(key: string, defaultValue: decimal) =
        match values.TryGetValue(key) with
        | true, v ->
            match Decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | true, res -> res
            | _ -> defaultValue
        | _ -> defaultValue

    member this.GetBoolean(key: string, defaultValue: bool) =
        match values.TryGetValue(key) with
        | true, v -> v.Equals("true", StringComparison.OrdinalIgnoreCase)
        | _ -> defaultValue

    member this.GetTimeSpan(key: string, defaultValue: Nullable<TimeSpan>) =
        match values.TryGetValue(key) with
        | true, v ->
            match TimeSpan.TryParse(v) with
            | true, res -> res
            | _ -> if defaultValue.HasValue then defaultValue.Value else TimeSpan.Zero
        | _ -> if defaultValue.HasValue then defaultValue.Value else TimeSpan.Zero

    member this.ContainsKey(key: string) = values.ContainsKey(key)

    member this.GetRaw(key: string) =
        match values.TryGetValue(key) with
        | true, v -> v
        | _ -> null

type ParameterSchema() =
    let mutable parameters = ResizeArray<ParameterDefinition>()
    let mutable orderCounter = 0

    member this.Parameters: IReadOnlyList<ParameterDefinition> = parameters.AsReadOnly()

    member this.AddString(key: string, displayName: string, ?description: string, ?required: bool, ?defaultValue: string, ?group: string) =
        parameters.Add(
            { Key = key
              DisplayName = displayName
              Description = defaultArg description null
              Type = ParameterType.String
              IsRequired = defaultArg required false
              DefaultValue = defaultArg defaultValue null
              Min = Nullable()
              Max = Nullable()
              Options = null
              Group = defaultArg group null
              Order = orderCounter }
        )

        orderCounter <- orderCounter + 1
        this

    member this.AddInteger
        (key: string, displayName: string, ?description: string, ?required: bool, ?defaultValue: int, ?min: int, ?max: int, ?group: string)
        =
        parameters.Add(
            { Key = key
              DisplayName = displayName
              Description = defaultArg description null
              Type = ParameterType.Integer
              IsRequired = defaultArg required false
              DefaultValue = defaultValue |> Option.map string |> Option.defaultValue null
              Min = min |> Option.map decimal |> Option.toNullable
              Max = max |> Option.map decimal |> Option.toNullable
              Options = null
              Group = defaultArg group null
              Order = orderCounter }
        )

        orderCounter <- orderCounter + 1
        this

    member this.AddDecimal
        (
            key: string,
            displayName: string,
            ?description: string,
            ?required: bool,
            ?defaultValue: decimal,
            ?min: decimal,
            ?max: decimal,
            ?group: string
        ) =
        parameters.Add(
            { Key = key
              DisplayName = displayName
              Description = defaultArg description null
              Type = ParameterType.Decimal
              IsRequired = defaultArg required false
              DefaultValue =
                defaultValue
                |> Option.map (fun d -> d.ToString(CultureInfo.InvariantCulture))
                |> Option.defaultValue null
              Min = min |> Option.toNullable
              Max = max |> Option.toNullable
              Options = null
              Group = defaultArg group null
              Order = orderCounter }
        )

        orderCounter <- orderCounter + 1
        this

    member this.AddBoolean(key: string, displayName: string, ?description: string, ?defaultValue: bool, ?group: string) =
        parameters.Add(
            { Key = key
              DisplayName = displayName
              Description = defaultArg description null
              Type = ParameterType.Boolean
              IsRequired = false
              DefaultValue = defaultValue |> Option.map (fun b -> b.ToString().ToLowerInvariant()) |> Option.defaultValue "false"
              Min = Nullable()
              Max = Nullable()
              Options = null
              Group = defaultArg group null
              Order = orderCounter }
        )

        orderCounter <- orderCounter + 1
        this

    member this.AddTimeSpan
        (key: string, displayName: string, ?description: string, ?required: bool, ?defaultValue: TimeSpan, ?group: string)
        =
        parameters.Add(
            { Key = key
              DisplayName = displayName
              Description = defaultArg description null
              Type = ParameterType.TimeSpan
              IsRequired = defaultArg required false
              DefaultValue = defaultValue |> Option.map (fun t -> t.ToString(@"hh\:mm\:ss")) |> Option.defaultValue null
              Min = Nullable()
              Max = Nullable()
              Options = null
              Group = defaultArg group null
              Order = orderCounter }
        )

        orderCounter <- orderCounter + 1
        this

    member this.AddSelect
        (
            key: string,
            displayName: string,
            options: IEnumerable<SelectOption>,
            ?description: string,
            ?required: bool,
            ?defaultValue: string,
            ?group: string
        ) =
        parameters.Add(
            { Key = key
              DisplayName = displayName
              Description = defaultArg description null
              Type = ParameterType.Select
              IsRequired = defaultArg required false
              DefaultValue = defaultArg defaultValue null
              Min = Nullable()
              Max = Nullable()
              Options = options |> Seq.toList |> (fun l -> l :> IReadOnlyList<SelectOption>)
              Group = defaultArg group null
              Order = orderCounter }
        )

        orderCounter <- orderCounter + 1
        this

    member this.GetDefaultValues() =
        let defaults = Dictionary<string, string>()

        for param in parameters do
            if not (String.IsNullOrEmpty(param.DefaultValue)) then
                defaults.[param.Key] <- param.DefaultValue

        defaults

    member this.Validate(values: IReadOnlyDictionary<string, string>) =
        let errors = ResizeArray<ValidationError>()

        for param in parameters do
            let hasValue, value = values.TryGetValue(param.Key)
            let isPresent = hasValue && not (String.IsNullOrWhiteSpace(value))

            if param.IsRequired && not isPresent then
                errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} is required." })
            elif not isPresent then
                () // Skip checks if optional and missing
            else
                match param.Type with
                | ParameterType.Integer ->
                    match Int32.TryParse(value) with
                    | false, _ -> errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be a whole number." })
                    | true, intVal ->
                        if param.Min.HasValue && decimal intVal < param.Min.Value then
                            errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be at least {param.Min.Value}." })

                        if param.Max.HasValue && decimal intVal > param.Max.Value then
                            errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be at most {param.Max.Value}." })
                | ParameterType.Decimal ->
                    match Decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture) with
                    | false, _ -> errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be a number." })
                    | true, decVal ->
                        if param.Min.HasValue && decVal < param.Min.Value then
                            errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be at least {param.Min.Value}." })

                        if param.Max.HasValue && decVal > param.Max.Value then
                            errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be at most {param.Max.Value}." })
                | ParameterType.Boolean ->
                    if value <> "true" && value <> "false" then
                        errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be true or false." })
                | ParameterType.TimeSpan ->
                    match TimeSpan.TryParse(value) with
                    | false, _ ->
                        errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be a valid time span (e.g., 00:05:00)." })
                    | _ -> ()
                | ParameterType.Select ->
                    if param.Options <> null && param.Options |> Seq.forall (fun x -> x.Value <> value) then
                        errors.Add({ Key = param.Key; Message = $"%s{param.DisplayName} must be one of the available options." })
                | _ -> ()

        { IsValid = errors.Count = 0; Errors = errors.AsReadOnly() }

type StepCategory =
    | Validation = 0
    | Risk = 1
    | Signal = 2
    | Execution = 3

type IPipelineContext =
    abstract member ExecutionId: Guid with get
    abstract member StartedAt: DateTime with get
    abstract member IsCancelled: bool with get, set

type IPipelineStep<'TContext when 'TContext :> IPipelineContext> =
    abstract member Type: PipelineStepType with get
    abstract member Name: string with get
    abstract member Parameters: Dictionary<string, string> with get
    abstract member ExecuteAsync: context: 'TContext * cancellationToken: CancellationToken -> Task<PipelineStepResult>

type IPipelineExecutor =
    abstract member PipelineId: int with get
    abstract member IsRunning: bool with get
    abstract member StartAsync: ct: CancellationToken -> Task
    abstract member StopAsync: unit -> Task

// Forward declaration needed typically? No, they are interfaces.
// But TradingContext is generic argument in some cases.
// Wait, TradingContext is defined in Core/Pipelines/Trading.
// It is NOT in Functional yet.
// IPipelineStep<TradingContext> is used in registered steps.
// However, the interface definition `IPipelineStep<TContext>` is generic.
// Usage in `IPipelineBuilder` was `IReadOnlyList<IPipelineStep<TradingContext>>`.
// Creating `IPipelineBuilder` here would require `TradingContext` to be visible or generic.
// Or `TradingContext` should move to Functional/Pipelines/Domain using `IPipelineContext`.
// Let's assume generic in definition unless strict.
// But `BuildSteps` returns `IReadOnlyList<IPipelineStep<TradingContext>>` in C# interface.
// If I move `IPipelineBuilder` interface here, I MUST resolve `TradingContext`.
// `TradingContext` is a class in `Warehouse.Core/Pipelines/Trading/TradingContext.cs`.
// It implements `IPipelineContext`.
// I should probably move `TradingContext` to `Warehouse.Core.Functional.Pipelines.Domain` as well.
// Let's check if I can define it here or reference it?
// Circular dependency risk: If `TradingContext` stays in Core, Functional cannot reference it.
// So `TradingContext` MUST move to Functional.

type IStepDefinition =
    abstract member Key: string with get
    abstract member Name: string with get
    abstract member Description: string with get
    abstract member Category: StepCategory with get
    abstract member Icon: string with get
    abstract member GetParameterSchema: unit -> ParameterSchema

type IStepDefinition<'TContext when 'TContext :> IPipelineContext> =
    inherit IStepDefinition
    abstract member CreateInstance: services: IServiceProvider * parameters: ParameterBag -> IPipelineStep<'TContext>

type IPipelineBuilder =
    abstract member BuildSteps: pipeline: Pipeline * services: IServiceProvider -> IReadOnlyList<IPipelineStep<IPipelineContext>>
    abstract member ValidatePipeline: pipeline: Pipeline -> ValidationResult

type IStepRegistry =
    abstract member GetAllDefinitions: unit -> IReadOnlyList<IStepDefinition>
    abstract member GetDefinition: key: string -> IStepDefinition option
    abstract member GetByCategory: category: StepCategory -> IEnumerable<IStepDefinition>
    abstract member CreateInstance: key: string * services: IServiceProvider * parameters: ParameterBag -> IPipelineStep<IPipelineContext> // Generic?
    abstract member ValidateParameters: key: string * parameters: IReadOnlyDictionary<string, string> -> ValidationResult

type IPipelineExecutorFactory =
    abstract member Create: pipeline: Pipeline * serviceProvider: IServiceProvider -> IPipelineExecutor
