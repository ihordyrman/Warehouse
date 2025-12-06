using System.Globalization;

namespace Warehouse.Core.Pipelines.Parameters;

/// <summary>
///     Type-safe wrapper for parameter values stored as strings.
/// </summary>
public class ParameterBag
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public ParameterBag(IReadOnlyDictionary<string, string> values) => _values = values;

    /// <summary>
    ///     Gets a string value.
    /// </summary>
    public string GetString(string key, string defaultValue = "") => _values.TryGetValue(key, out string? value) ? value : defaultValue;

    /// <summary>
    ///     Gets an integer value.
    /// </summary>
    public int GetInteger(string key, int defaultValue = 0)
    {
        if (_values.TryGetValue(key, out string? value) && int.TryParse(value, out int result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Gets a decimal value.
    /// </summary>
    public decimal GetDecimal(string key, decimal defaultValue = 0m)
    {
        if (_values.TryGetValue(key, out string? value) &&
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Gets a boolean value.
    /// </summary>
    public bool GetBoolean(string key, bool defaultValue = false)
    {
        if (_values.TryGetValue(key, out string? value))
        {
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return defaultValue;
    }

    /// <summary>
    ///     Gets a TimeSpan value.
    /// </summary>
    public TimeSpan GetTimeSpan(string key, TimeSpan? defaultValue = null)
    {
        if (_values.TryGetValue(key, out string? value) && TimeSpan.TryParse(value, out TimeSpan result))
        {
            return result;
        }

        return defaultValue ?? TimeSpan.Zero;
    }

    /// <summary>
    ///     Checks if a key exists in the bag.
    /// </summary>
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <summary>
    ///     Gets the raw string value, or null if not present.
    /// </summary>
    public string? GetRaw(string key) => _values.TryGetValue(key, out string? value) ? value : null;
}
