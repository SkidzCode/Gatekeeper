using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Attributes;

public class MustBeTrueAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is bool booleanValue && booleanValue)
        {
            return ValidationResult.Success;
        }
        return new ValidationResult("The field must be true.");
    }
}
