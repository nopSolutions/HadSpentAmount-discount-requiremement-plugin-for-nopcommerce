using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.DiscountRules.HadSpentAmount
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            routeBuilder.MapRoute("Plugin.DiscountRules.HadSpentAmount.Configure",
                 "Plugins/DiscountRulesHadSpentAmount/Configure",
                 new { controller = "DiscountRulesHadSpentAmount", action = "Configure" });
        }

        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
