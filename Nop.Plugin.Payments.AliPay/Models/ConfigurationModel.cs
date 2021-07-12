using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.AliPay.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.AliPay.SellerEmail")]
        public string SellerEmail { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.Key")]
        public string Key { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.Partner")]
        public string Partner { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
    }
}