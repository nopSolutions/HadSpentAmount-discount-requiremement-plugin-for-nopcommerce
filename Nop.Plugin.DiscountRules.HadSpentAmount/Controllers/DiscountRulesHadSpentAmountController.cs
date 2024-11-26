using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.HadSpentAmount.Models;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.DiscountRules.HadSpentAmount.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class DiscountRulesHadSpentAmountController : BasePluginController
{
    #region Fields

    private readonly IDiscountService _discountService;
    private readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public DiscountRulesHadSpentAmountController(IDiscountService discountService,
        ISettingService settingService)
    {
        _discountService = discountService;
        _settingService = settingService;
    }

    #endregion

    #region Utilities

    private IEnumerable<string> GetErrorsFromModelState()
    {
        return ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_VIEW)]
    public async Task<IActionResult> Configure(int discountId, int? discountRequirementId)
    {
        var discount = await _discountService.GetDiscountByIdAsync(discountId) ?? throw new ArgumentException("Discount could not be loaded");

        //check whether the discount requirement exists
        if (discountRequirementId.HasValue && await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId.Value) is null)
            return Content("Failed to load requirement.");

        var spentAmountRequirement = await _settingService.GetSettingByKeyAsync<decimal>(string.Format(DiscountRequirementDefaults.SETTINGS_KEY, discountRequirementId ?? 0));

        var model = new RequirementModel
        {
            RequirementId = discountRequirementId ?? 0,
            DiscountId = discount.Id,
            SpentAmount = spentAmountRequirement
        };

        //add a prefix
        ViewData.TemplateInfo.HtmlFieldPrefix = string.Format(DiscountRequirementDefaults.HTML_FIELD_PREFIX, discountRequirementId ?? 0);

        return View("~/Plugins/DiscountRules.HadSpentAmount/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> Configure(RequirementModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Errors = GetErrorsFromModelState() });

        //load the discount
        var discount = await _discountService.GetDiscountByIdAsync(model.DiscountId);
        if (discount == null)
            return NotFound(new { Errors = new[] { "Discount could not be loaded" } });

        //get the discount requirement
        var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(model.RequirementId);

        //the discount requirement does not exist, so create a new one
        if (discountRequirement == null)
        {
            discountRequirement = new DiscountRequirement
            {
                DiscountId = discount.Id,
                DiscountRequirementRuleSystemName = DiscountRequirementDefaults.SYSTEM_NAME
            };

            await _discountService.InsertDiscountRequirementAsync(discountRequirement);
        }

        //save restricted customer role identifier
        await _settingService.SetSettingAsync(string.Format(DiscountRequirementDefaults.SETTINGS_KEY, discountRequirement.Id), model.SpentAmount);

        return Ok(new { NewRequirementId = discountRequirement.Id });
    }

    #endregion
}