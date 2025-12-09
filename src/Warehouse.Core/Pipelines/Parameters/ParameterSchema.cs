using System.Globalization;

namespace Warehouse.Core.Pipelines.Parameters;

/// <summary>
///     Fluent builder for defining step parameter schemas.
/// </summary>
public class ParameterSchema
{
    private readonly List<ParameterDefinition> parameters = [];
    private int orderCounter;

    /// <summary>
    ///     Gets all parameter definitions in this schema.
    /// </summary>
    public IReadOnlyList<ParameterDefinition> Parameters => parameters.AsReadOnly();

    /// <summary>
    ///     Adds a string parameter.
    /// </summary>
    public ParameterSchema AddString(
        string key,
        string displayName,
        string? description = null,
        bool required = false,
        string? defaultValue = null,
        string? group = null)
    {
        parameters.Add(
            new ParameterDefinition
            {
                Key = key,
                DisplayName = displayName,
                Description = description,
                Type = ParameterType.String,
                IsRequired = required,
                DefaultValue = defaultValue,
                Group = group,
                Order = orderCounter++
            });
        return this;
    }

    /// <summary>
    ///     Adds an integer parameter.
    /// </summary>
    public ParameterSchema AddInteger(
        string key,
        string displayName,
        string? description = null,
        bool required = false,
        int? defaultValue = null,
        int? min = null,
        int? max = null,
        string? group = null)
    {
        parameters.Add(
            new ParameterDefinition
            {
                Key = key,
                DisplayName = displayName,
                Description = description,
                Type = ParameterType.Integer,
                IsRequired = required,
                DefaultValue = defaultValue?.ToString(),
                Min = min,
                Max = max,
                Group = group,
                Order = orderCounter++
            });
        return this;
    }

    /// <summary>
    ///     Adds a decimal parameter.
    /// </summary>
    public ParameterSchema AddDecimal(
        string key,
        string displayName,
        string? description = null,
        bool required = false,
        decimal? defaultValue = null,
        decimal? min = null,
        decimal? max = null,
        string? group = null)
    {
        parameters.Add(
            new ParameterDefinition
            {
                Key = key,
                DisplayName = displayName,
                Description = description,
                Type = ParameterType.Decimal,
                IsRequired = required,
                DefaultValue = defaultValue?.ToString(CultureInfo.InvariantCulture),
                Min = min,
                Max = max,
                Group = group,
                Order = orderCounter++
            });
        return this;
    }

    /// <summary>
    ///     Adds a boolean parameter.
    /// </summary>
    public ParameterSchema AddBoolean(
        string key,
        string displayName,
        string? description = null,
        bool defaultValue = false,
        string? group = null)
    {
        parameters.Add(
            new ParameterDefinition
            {
                Key = key,
                DisplayName = displayName,
                Description = description,
                Type = ParameterType.Boolean,
                IsRequired = false, // Booleans are never "required" in the traditional sense
                DefaultValue = defaultValue.ToString().ToLowerInvariant(),
                Group = group,
                Order = orderCounter++
            });
        return this;
    }

    /// <summary>
    ///     Adds a timespan parameter.
    /// </summary>
    public ParameterSchema AddTimeSpan(
        string key,
        string displayName,
        string? description = null,
        bool required = false,
        TimeSpan? defaultValue = null,
        string? group = null)
    {
        parameters.Add(
            new ParameterDefinition
            {
                Key = key,
                DisplayName = displayName,
                Description = description,
                Type = ParameterType.TimeSpan,
                IsRequired = required,
                DefaultValue = defaultValue?.ToString(@"hh\:mm\:ss"),
                Group = group,
                Order = orderCounter++
            });
        return this;
    }

    /// <summary>
    ///     Adds a select parameter with predefined options.
    /// </summary>
    public ParameterSchema AddSelect(
        string key,
        string displayName,
        IEnumerable<SelectOption> options,
        string? description = null,
        bool required = false,
        string? defaultValue = null,
        string? group = null)
    {
        parameters.Add(
            new ParameterDefinition
            {
                Key = key,
                DisplayName = displayName,
                Description = description,
                Type = ParameterType.Select,
                IsRequired = required,
                DefaultValue = defaultValue,
                Options = options.ToList().AsReadOnly(),
                Group = group,
                Order = orderCounter++
            });
        return this;
    }

    /// <summary>
    ///     Gets the default values for all parameters.
    /// </summary>
    public Dictionary<string, string> GetDefaultValues()
    {
        var defaults = new Dictionary<string, string>();
        foreach (ParameterDefinition param in parameters)
        {
            if (param.DefaultValue is not null)
            {
                defaults[param.Key] = param.DefaultValue;
            }
        }

        return defaults;
    }

    /// <summary>
    ///     Validates parameter values against the schema.
    /// </summary>
    public ValidationResult Validate(IReadOnlyDictionary<string, string> values)
    {
        var errors = new List<ValidationError>();

        foreach (ParameterDefinition param in parameters)
        {
            bool hasValue = values.TryGetValue(param.Key, out string? value) && !string.IsNullOrWhiteSpace(value);

            // Required check
            if (param.IsRequired && !hasValue)
            {
                errors.Add(new ValidationError(param.Key, $"{param.DisplayName} is required."));
                continue;
            }

            if (!hasValue)
            {
                continue;
            }

            // Type-specific validation
            switch (param.Type)
            {
                case ParameterType.Integer:
                    if (!int.TryParse(value, out int intVal))
                    {
                        errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be a whole number."));
                    }
                    else
                    {
                        if (param.Min.HasValue && intVal < param.Min.Value)
                        {
                            errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be at least {param.Min.Value}."));
                        }

                        if (param.Max.HasValue && intVal > param.Max.Value)
                        {
                            errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be at most {param.Max.Value}."));
                        }
                    }

                    break;

                case ParameterType.Decimal:
                    if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decVal))
                    {
                        errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be a number."));
                    }
                    else
                    {
                        if (param.Min.HasValue && decVal < param.Min.Value)
                        {
                            errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be at least {param.Min.Value}."));
                        }

                        if (param.Max.HasValue && decVal > param.Max.Value)
                        {
                            errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be at most {param.Max.Value}."));
                        }
                    }

                    break;

                case ParameterType.Boolean:
                    if (value != "true" && value != "false")
                    {
                        errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be true or false."));
                    }

                    break;

                case ParameterType.TimeSpan:
                    if (!TimeSpan.TryParse(value, out _))
                    {
                        errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be a valid time span (e.g., 00:05:00)."));
                    }

                    break;

                case ParameterType.Select:
                    if (param.Options is not null && param.Options.All(x => x.Value != value))
                    {
                        errors.Add(new ValidationError(param.Key, $"{param.DisplayName} must be one of the available options."));
                    }

                    break;
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
///     Result of parameter validation.
/// </summary>
public record ValidationResult(bool IsValid, IReadOnlyList<ValidationError> Errors);

/// <summary>
///     A single validation error.
/// </summary>
public record ValidationError(string Key, string Message);
