using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <returns>Result</returns>
        public DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //invalid by default
            var result = new DiscountRequirementValidationResult();

            var spentAmountRequirement = _settingService.GetSettingByKey<decimal>($"DiscountRequirement.HadSpentAmount-{request.DiscountRequirementId}");
            if (spentAmountRequirement == decimal.Zero)
            {
                //valid
                result.IsValid = true;
                return result;
            }

            if (request.Customer == null || _customerService.IsGuest(request.Customer))
                return result;

            var orders = _orderService.SearchOrders(request.Store.Id,
                customerId: request.Customer.Id,
                osIds: new List<int> { (int)OrderStatus.Complete });
            var spentAmount = orders.Sum(o => o.OrderTotal);
            if (spentAmount > spentAmountRequirement)
            {
                result.IsValid = true;
            }
            else
            {
                result.UserError = _localizationService.GetResource("Plugins.DiscountRules.HadSpentAmount.NotEnough");
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
                new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.CurrentRequestProtocol);
        }

        public override void Install()
        {
            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.DiscountRules.HadSpentAmount.Fields.Amount"] = "Required spent amount",
                ["Plugins.DiscountRules.HadSpentAmount.Fields.Amount.Hint"] = "Discount will be applied if customer has spent/purchased x.xx amount.",
                ["Plugins.DiscountRules.HadSpentAmount.NotEnough"] = "Sorry, this offer requires more money spent (previously placed orders)",
                ["Plugins.DiscountRules.HadSpentAmount.Fields.SpentAmount.Required"] = "Spent amount should be greater 0",
                ["Plugins.DiscountRules.HadSpentAmount.Fields.DiscountId.Required"] = "Discount is required"
            });

            base.Install();
        }

        public override void Uninstall()
        {
            //discount requirements
            var discountRequirements = _discountService.GetAllDiscountRequirements()
                .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountRequirementDefaults.SYSTEM_NAME);
            foreach (var discountRequirement in discountRequirements)
            {
                _discountService.DeleteDiscountRequirement(discountRequirement, false);
            }

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.DiscountRules.HadSpentAmount");

            base.Uninstall();
        }
    }
}