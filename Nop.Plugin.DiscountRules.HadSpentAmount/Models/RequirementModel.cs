using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.DiscountRules.HadSpentAmount.Models
{
    public class RequirementModel
    {
        public int DiscountId { get; set; }

        public int RequirementId { get; set; }

        [NopResourceDisplayName("Plugins.DiscountRules.HadSpentAmount.Fields.Amount")]
        public decimal SpentAmount { get; set; }
    }
}