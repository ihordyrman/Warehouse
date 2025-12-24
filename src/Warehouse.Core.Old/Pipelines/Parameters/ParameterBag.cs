using System.Globalization;

namespace Warehouse.Core.Old.Pipelines.Parameters;

/// <summary>
///     Type-safe wrapper for parameter values stored as strings.
/// </summary>
public class ParameterBag(IReadOnlyDictionary<string, string> values)
{
    /// <summary>
    ///     Gets a string value.
    /// </summary>
    public string GetString(string key, string defaultValue = "") => values.GetValueOrDefault(key, defaultValue);

    /// <summary>
    ///     Gets an integer value.
    /// </summary>
    public int GetInteger(string key, int defaultValue = 0)
    {
        if (values.TryGetValue(key, out string? value) &&
            int.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out int result))
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
        if (values.TryGetValue(key, out string? value) &&
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
        => values.TryGetValue(key, out string? value) ? value.Equals("true", StringComparison.OrdinalIgnoreCase) : defaultValue;

    /// <summary>
    ///     Gets a TimeSpan value.
    /// </summary>
    public TimeSpan GetTimeSpan(string key, TimeSpan? defaultValue = null)
        => values.TryGetValue(key, out string? value) && TimeSpan.TryParse(value, out TimeSpan result) ?
            result :
            defaultValue ?? TimeSpan.Zero;

    /// <summary>
    ///     Checks if a key exists in the bag.
    /// </summary>
    public bool ContainsKey(string key) => values.ContainsKey(key);

    /// <summary>
    ///     Gets the raw string value, or null if not present.
    /// </summary>
    public string? GetRaw(string key) => values.GetValueOrDefault(key);
}
