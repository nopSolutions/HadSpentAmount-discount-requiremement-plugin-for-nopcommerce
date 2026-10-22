using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.DiscountRules.HadSpentAmount;

public class HadSpentAmountDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
{
    #region Fields

    private readonly ICustomerService _customerService;
    private readonly IDiscountService _discountService;
    private readonly ILocalizationService _localizationService;
    private readonly INopUrlHelper _nopUrlHelper;
    private readonly IOrderService _orderService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public HadSpentAmountDiscountRequirementRule(
        ICustomerService customerService,
        IDiscountService discountService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IOrderService orderService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _customerService = customerService;
        _discountService = discountService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _orderService = orderService;
        _settingService = settingService;
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Check discount requirement
    /// </summary>
    /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc.)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public async Task<DiscountRequirementValidationResult> CheckRequirementAsync(DiscountRequirementValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        //invalid by default
        var result = new DiscountRequirementValidationResult();

        var spentAmountRequirement = await _settingService.GetSettingByKeyAsync<decimal>($"DiscountRequirement.HadSpentAmount-{request.DiscountRequirementId}");
        
        if (spentAmountRequirement == decimal.Zero)
        {
            //valid
            result.IsValid = true;
            return result;
        }

        if (request.Customer == null || await _customerService.IsGuestAsync(request.Customer))
            return result;

        var orders = await _orderService.SearchOrdersAsync(request.Store.Id,
            customerId: request.Customer.Id,
            osIds: [(int)OrderStatus.Complete]);
        var spentAmount = orders.Sum(o => o.OrderTotal);
        
        if (spentAmount > spentAmountRequirement)
            result.IsValid = true;
        else
            result.UserError = await _localizationService.GetResourceAsync("Plugins.DiscountRules.HadSpentAmount.NotEnough");

        return result;
    }

    /// <summary>
    /// Get URL for rule configuration
    /// </summary>
    /// <param name="discountId">Discount identifier</param>
    /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
    /// <returns>URL</returns>
    public string GetConfigurationUrl(int discountId, int? discountRequirementId)
    {
        return _nopUrlHelper.RouteUrl(DiscountRequirementDefaults.ConfigurationRouteName,
            new { discountId, discountRequirementId }, _webHelper.GetCurrentRequestProtocol());
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.DiscountRules.HadSpentAmount.Fields.Amount"] = "Required spent amount",
            ["Plugins.DiscountRules.HadSpentAmount.Fields.Amount.Hint"] = "Discount will be applied if customer has spent/purchased x.xx amount.",
            ["Plugins.DiscountRules.HadSpentAmount.NotEnough"] = "Sorry, this offer requires more money spent (previously placed orders)",
            ["Plugins.DiscountRules.HadSpentAmount.Fields.SpentAmount.Required"] = "Spent amount should be greater 0",
            ["Plugins.DiscountRules.HadSpentAmount.Fields.DiscountId.Required"] = "Discount is required"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        //discount requirements
        var discountRequirements = (await _discountService.GetAllDiscountRequirementsAsync())
            .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountRequirementDefaults.SYSTEM_NAME);

        foreach (var discountRequirement in discountRequirements)
            await _discountService.DeleteDiscountRequirementAsync(discountRequirement, false);

        //locales
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.DiscountRules.HadSpentAmount");

        await base.UninstallAsync();
    }

    #endregion
}