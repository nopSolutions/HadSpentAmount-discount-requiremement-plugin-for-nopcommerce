using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;

namespace Nop.Plugin.DiscountRules.HadSpentAmount
{
    public partial class HadSpentAmountDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IWebHelper _webHelper;

        public HadSpentAmountDiscountRequirementRule(IActionContextAccessor actionContextAccessor,
            ICustomerService customerService,
            IDiscountService discountService,
            ILocalizationService localizationService,
            IOrderService orderService,
            ISettingService settingService,            
            IUrlHelperFactory urlHelperFactory,            
            IWebHelper webHelper)
        {
            _actionContextAccessor = actionContextAccessor;
            _customerService = customerService;
            _discountService = discountService;
            _localizationService = localizationService;
            _orderService = orderService;
            _settingService = settingService;                        
            _urlHelperFactory = urlHelperFactory;
            _webHelper = webHelper;
        }

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public async Task<DiscountRequirementValidationResult> CheckRequirementAsync(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

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
                osIds: new List<int> { (int)OrderStatus.Complete });
            var spentAmount = orders.Sum(o => o.OrderTotal);
            if (spentAmount > spentAmountRequirement)
            {
                result.IsValid = true;
            }
            else
            {
                result.UserError = await _localizationService.GetResourceAsync("Plugins.DiscountRules.HadSpentAmount.NotEnough");
            }

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
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            return urlHelper.Action("Configure", "DiscountRulesHadSpentAmount",
                new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.GetCurrentRequestProtocol());
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
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
            {
                await _discountService.DeleteDiscountRequirementAsync(discountRequirement, false);
            }

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.DiscountRules.HadSpentAmount");

            await base.UninstallAsync();
        }
    }
}