namespace Warehouse.Core.Pipelines.Steps;

/// <summary>
///     Attribute for marking step definition classes for automatic discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class StepDefinitionAttribute(string key) : Attribute
{
    /// <summary>
    ///     The unique key for this step definition.
    /// </summary>
    public string Key { get; } = key;
}
