using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;

namespace Nop.Plugin.DiscountRules.HadSpentAmount
{
    public partial class HadSpentAmountDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;

        public HadSpentAmountDiscountRequirementRule(ILocalizationService localizationService,
            ISettingService settingService, 
            IOrderService orderService,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor)
        {
            this._localizationService = localizationService;
            this._settingService = settingService;
            this._orderService = orderService;
            this._actionContextAccessor = actionContextAccessor;
            this._urlHelperFactory = urlHelperFactory;
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

            if (request.Customer == null || request.Customer.IsGuest())
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
            var url = new PathString(urlHelper.Action("Configure", "DiscountRulesHadSpentAmount",
                new { discountId = discountId, discountRequirementId = discountRequirementId }));

            //remove the application path from the generated URL if exists
            var pathBase = _actionContextAccessor.ActionContext?.HttpContext?.Request?.PathBase ?? PathString.Empty;
            url.StartsWithSegments(pathBase, out url);

            return url.Value.TrimStart('/');
        }

        public override void Install()
        {
            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.HadSpentAmount.Fields.Amount", "Required spent amount");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.HadSpentAmount.Fields.Amount.Hint", "Discount will be applied if customer has spent/purchased x.xx amount.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.HadSpentAmount.NotEnough", "Sorry, this offer requires more money spent (previously placed orders)");
            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.DiscountRules.HadSpentAmount.Fields.Amount");
            _localizationService.DeletePluginLocaleResource("Plugins.DiscountRules.HadSpentAmount.Fields.Amount.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.DiscountRules.HadSpentAmount.NotEnough");
            base.Uninstall();
        }
    }
}