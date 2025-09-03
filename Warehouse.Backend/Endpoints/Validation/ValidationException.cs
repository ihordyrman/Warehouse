using System.ComponentModel.DataAnnotations;

namespace Warehouse.Backend.Endpoints.Validation;

public class ValidationException : Exception
{
    public ValidationException(ValidationResult validationResult) : base(validationResult.ErrorMessage)
        => ValidationResults = [validationResult];

    public ValidationException(List<ValidationResult> validationResults) : base("Validation failed")
        => ValidationResults = validationResults;

    public ValidationException(string message, string propertyName) : base(message)
        => ValidationResults = [new ValidationResult(message, [propertyName])];

    public ValidationException(string message) : base(message) => ValidationResults = [new ValidationResult(message)];

    public List<ValidationResult> ValidationResults { get; }

    public ValidationResult ValidationResult => ValidationResults.FirstOrDefault() ?? new ValidationResult("Validation failed");
}
