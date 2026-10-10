using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ParkingApi.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ParkingSpotNumberAttribute : ValidationAttribute
{
    private static readonly Regex Pattern = new("\\AP-(?:0[1-9]|[1-9][0-9])\\z", RegexOptions.Compiled);

    public ParkingSpotNumberAttribute()
    {
        ErrorMessage = "Номер места должен быть в формате P-01—P-99 (например, P-12).";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success; // Пустое значение отдельно проверяет [Required].
        }

        return value is string number && Pattern.IsMatch(number)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}
