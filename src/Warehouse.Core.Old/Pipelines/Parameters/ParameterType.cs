namespace Warehouse.Core.Old.Pipelines.Parameters;

/// <summary>
///     Defines the data types supported for step parameters.
/// </summary>
public enum ParameterType
{
    /// <summary>
    ///     A text string value.
    /// </summary>
    String,

    /// <summary>
    ///     A whole number value.
    /// </summary>
    Integer,

    /// <summary>
    ///     A decimal number value.
    /// </summary>
    Decimal,

    /// <summary>
    ///     A true/false value.
    /// </summary>
    Boolean,

    /// <summary>
    ///     A time duration value (e.g., 00:05:00).
    /// </summary>
    TimeSpan,

    /// <summary>
    ///     A value selected from predefined options.
    /// </summary>
    Select
}
