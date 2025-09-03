using System.ComponentModel.DataAnnotations;

namespace Warehouse.Backend.Endpoints.Validation;

public static class ValidationHelper
{
    /// <summary>
    ///     Validates a model and throws ValidationException if invalid
    /// </summary>
    public static void ValidateAndThrow<T>(T model)
        where T : class
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);

        bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        if (!isValid)
        {
            throw new ValidationException(validationResults);
        }
    }
}
