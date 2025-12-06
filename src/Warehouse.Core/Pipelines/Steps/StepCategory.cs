namespace Warehouse.Core.Pipelines.Steps;

/// <summary>
///     Categories for pipeline steps, used for organization in the UI.
/// </summary>
public enum StepCategory
{
    /// <summary>
    ///     Steps that validate conditions before proceeding.
    /// </summary>
    Validation,

    /// <summary>
    ///     Steps that manage risk and position sizing.
    /// </summary>
    Risk,

    /// <summary>
    ///     Steps that generate trading signals.
    /// </summary>
    Signal,

    /// <summary>
    ///     Steps that execute trades.
    /// </summary>
    Execution
}
