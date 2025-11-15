using System.ComponentModel.DataAnnotations;

namespace Warehouse.App.Endpoints.Validation;

public class ValidEnumAttribute(Type enumType) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (!enumType.IsEnum)
        {
            return new ValidationResult($"{enumType.Name} is not an enum type.");
        }

        if (!Enum.IsDefined(enumType, value))
        {
            return new ValidationResult($"'{value}' is not a valid value for {enumType.Name}.");
        }

        return ValidationResult.Success;
    }
}
