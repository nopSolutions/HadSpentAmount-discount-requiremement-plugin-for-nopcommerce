using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.HadSpentAmount.Models;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.DiscountRules.HadSpentAmount.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class DiscountRulesHadSpentAmountController : BasePluginController
    {
        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        public DiscountRulesHadSpentAmountController(IDiscountService discountService,
            ISettingService settingService,
            IPermissionService permissionService)
        {
            _discountService = discountService;
            _permissionService = permissionService;
            _settingService = settingService;

            // little hack here
            //always set culture to 'en-US' (Telerik has a bug related to editing decimal values in other cultures). Like currently it's done for admin area in Global.asax.cs
            CommonHelper.SetTelerikCulture();
        }

        public IActionResult Configure(int discountId, int? discountRequirementId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            if (discountRequirementId.HasValue)
            {
                var discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);
                if (discountRequirement == null)
                    return Content("Failed to load requirement.");
            }

            var spentAmountRequirement = _settingService.GetSettingByKey<decimal>($"DiscountRequirement.HadSpentAmount-{discountRequirementId ?? 0}");

            var model = new RequirementModel
            {
                RequirementId = discountRequirementId ?? 0,
                DiscountId = discountId,
                SpentAmount = spentAmountRequirement
            };

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = $"DiscountRulesHadSpentAmount{discountRequirementId?.ToString() ?? "0"}";

            return View("~/Plugins/DiscountRules.HadSpentAmount/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public IActionResult Configure(int discountId, int? discountRequirementId, decimal spentAmount)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            DiscountRequirement discountRequirement = null;
            if (discountRequirementId.HasValue)
                discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);

            if (discountRequirement != null)
            {
                //update existing rule
                _settingService.SetSetting($"DiscountRequirement.HadSpentAmount-{discountRequirement.Id}", spentAmount);
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRequirement
                {
                    DiscountRequirementRuleSystemName = "DiscountRequirement.HadSpentAmount"
                };
                discount.DiscountRequirements.Add(discountRequirement);
                _discountService.UpdateDiscount(discount);

                _settingService.SetSetting($"DiscountRequirement.HadSpentAmount-{discountRequirement.Id}", spentAmount);
            }
            return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
        }
    }
}