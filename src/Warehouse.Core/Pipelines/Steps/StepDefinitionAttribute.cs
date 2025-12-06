namespace Warehouse.Core.Pipelines.Steps;

/// <summary>
///     Attribute for marking step definition classes for automatic discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class StepDefinitionAttribute : Attribute
{
    public StepDefinitionAttribute(string key) => Key = key;

    /// <summary>
    ///     The unique key for this step definition.
    /// </summary>
    public string Key { get; }
}
