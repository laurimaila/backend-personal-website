using System.ComponentModel.DataAnnotations;

namespace backend.Services;

public interface IValidationService
{
    (bool IsValid, ICollection<string> Errors) Validate<T>(T model) where T : class;
}

public class ValidationService : IValidationService
{
    public (bool IsValid, ICollection<string> Errors) Validate<T>(T model) where T : class
    {
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        var isValid =
            Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        return (isValid, validationResults.Select(r => r.ErrorMessage).ToList());
    }
}
