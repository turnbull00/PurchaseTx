namespace api.Validators;

using System.Globalization;

using api.Endpoints;

using FluentValidation;

internal class NewPurchaseValidator : AbstractValidator<PurchaseEndpoints.NewPurchase>
{
    public NewPurchaseValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Amount)
            .Must(amount => Math.Round(amount, 2) == amount)
            .WithMessage("Amount must not have more than 2 decimal places");

        RuleFor(x => x.Description)
            .MaximumLength(50)
            .WithMessage("Description must be 50 characters or less");

        RuleFor(x => x.Date)
            .Must(date => DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("Date must be a valid date");

        RuleFor(x => x.ClientPurchaseId)
            .NotEqual(Guid.Empty)
            .WithMessage("ClientPurchaseId is required");
    }
}
