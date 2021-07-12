using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.AliPay
{
    public partial class RouteProvider : IRouteProvider
    {
        #region Methods

        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.AliPay.Notify", "Plugins/PaymentAliPay/Notify",
                 new { controller = "PaymentAliPay", action = "Notify" });

            //Notify
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.AliPay.Return", "Plugins/PaymentAliPay/Return",
                 new { controller = "PaymentAliPay", action = "Return" });
        }

        #endregion

        #region Properties

        public int Priority
        {
            get
            {
                return 0;
            }
        }

        #endregion
    }
}
