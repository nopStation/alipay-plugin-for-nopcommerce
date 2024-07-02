using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.AliPay
{
    public partial class RouteProvider : IRouteProvider
    {
        #region Methods

        public void RegisterRoutes(IEndpointRouteBuilder routes)
        {
            routes.MapControllerRoute(
                name: "Plugin.Payments.AliPay.Notify",
                pattern: "Plugins/PaymentAliPay/Notify",
                defaults: new { controller = "PaymentAliPay", action = "Notify" });

            routes.MapControllerRoute(
                name: "Plugin.Payments.AliPay.Return",
                pattern: "Plugins/PaymentAliPay/Return",
                defaults: new { controller = "PaymentAliPay", action = "Return" });
        }

        #endregion

        #region Properties

        public int Priority => 0;

        #endregion
    }
}
