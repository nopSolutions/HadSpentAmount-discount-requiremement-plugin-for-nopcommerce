using FluentValidation;
using Nop.Plugin.DiscountRules.HadSpentAmount.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.DiscountRules.HadSpentAmount.Validators
{
    /// <summary>
    /// Represents an <see cref="RequirementModel"/> validator.
    /// </summary>
    public class RequirementModelValidator : BaseNopValidator<RequirementModel>
    {
        public RequirementModelValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.DiscountId)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.DiscountRules.HadSpentAmount.Fields.DiscountId.Required"));
            RuleFor(model => model.SpentAmount)
                .GreaterThan(0)
                .WithMessage(localizationService.GetResource("Plugins.DiscountRules.HadSpentAmount.Fields.SpentAmount.Required"));
        }
    }
}
