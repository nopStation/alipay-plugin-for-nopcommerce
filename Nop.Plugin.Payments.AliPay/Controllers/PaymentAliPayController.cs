using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.AliPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.AliPay.Controllers
{
    public class PaymentAliPayController : BasePaymentController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly AliPayPaymentSettings _aliPayPaymentSettings;
        private readonly IPermissionService _permissionService;
        private readonly IPaymentPluginManager _paymentPluginManager;

        #endregion

        #region Ctor

        public PaymentAliPayController(ISettingService settingService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger,
            INotificationService notificationService,
            ILocalizationService localizationService,
            AliPayPaymentSettings aliPayPaymentSettings,
            IPermissionService permissionService,
            IPaymentPluginManager paymentPluginManager)
        {
            _settingService = settingService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _logger = logger;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _aliPayPaymentSettings = aliPayPaymentSettings;
            _permissionService = permissionService;
            _paymentPluginManager = paymentPluginManager;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                SellerEmail = _aliPayPaymentSettings.SellerEmail,
                Key = _aliPayPaymentSettings.Key,
                Partner = _aliPayPaymentSettings.Partner,
                AdditionalFee = _aliPayPaymentSettings.AdditionalFee
            };

            return View("~/Plugins/Payments.AliPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //save settings
            _aliPayPaymentSettings.SellerEmail = model.SellerEmail;
            _aliPayPaymentSettings.Key = model.Key;
            _aliPayPaymentSettings.Partner = model.Partner;
            _aliPayPaymentSettings.AdditionalFee = model.AdditionalFee;

            await _settingService.SaveSettingAsync(_aliPayPaymentSettings);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return RedirectToAction("Configure");
        }
        
        public async Task<IActionResult> Notify(IFormCollection form) 
        {
            var processor = await _paymentPluginManager.LoadPluginBySystemNameAsync("Payments.AliPay") as AliPayPaymentProcessor;

            if (processor == null || !_paymentPluginManager.IsPluginActive(processor) || !processor.PluginDescriptor.Installed)
                throw new NopException("AliPay module cannot be loaded");

            var partner = _aliPayPaymentSettings.Partner;

            if (string.IsNullOrEmpty(partner))
                throw new Exception("Partner is not set");

            var key = _aliPayPaymentSettings.Key;

            if (string.IsNullOrEmpty(key))
                throw new Exception("Partner is not set");

            var alipayNotifyUrl = $"https://www.alipay.com/cooperate/gateway.do?service=notify_verify&partner={partner}&notify_id={form["notify_id"]}";

            var responseTxt = string.Empty;

            try
            {
                var myReq = (HttpWebRequest)WebRequest.Create(alipayNotifyUrl);
                myReq.Timeout = 120000;

                var httpWResp = (HttpWebResponse)myReq.GetResponse();
                var myStream = httpWResp.GetResponseStream();
                if (myStream != null)
                {
                    using (var sr = new StreamReader(myStream, Encoding.Default))
                    {
                        var strBuilder = new StringBuilder();

                        while (-1 != sr.Peek())
                        {
                            strBuilder.Append(sr.ReadLine());
                        }

                        responseTxt = strBuilder.ToString();
                    }
                }
            }
            catch (Exception exc)
            {
                responseTxt = $"Error: {exc.Message}";
            }

            int i;
            var sortedStr = form.Keys.ToArray();

            Array.Sort(sortedStr, StringComparer.InvariantCulture);
            var prestr = new StringBuilder();

            for (i = 0; i < sortedStr.Length; i++)
            {
                if (form[sortedStr[i]] == "" || sortedStr[i] == "sign" || sortedStr[i] == "sign_type")
                    continue;

                prestr.AppendFormat("{0}={1}", sortedStr[i], form[sortedStr[i]]);

                if (i < sortedStr.Length - 1)
                {
                    prestr.Append("&");
                }
            }

            prestr.Append(key);

            var mySign = processor.GetMD5(prestr.ToString());

            var sign = form["sign"];

            byte[] data = null;
            if (mySign == sign && responseTxt == "true")
            {
                if (form["trade_status"] == "TRADE_FINISHED" || form["trade_status"] == "TRADE_SUCCESS")
                {
                    var strOrderNo = form["out_trade_no"];
                    int orderId;

                    if (int.TryParse(strOrderNo, out orderId))
                    {
                        var order = await _orderService.GetOrderByIdAsync(orderId);

                        if (order != null && _orderProcessingService.CanMarkOrderAsPaid(order))
                        {
                            await _orderProcessingService.MarkOrderAsPaidAsync(order);
                        }
                    }
                }

                data = Encoding.UTF8.GetBytes("success");
                
            }
            else
            {
                data = Encoding.UTF8.GetBytes("fail");

                var logStr = $"MD5:mysign={mySign},sign={sign},responseTxt={responseTxt}";

                await _logger.ErrorAsync(logStr);
            }
            
            Response.Body.Write(data, 0, data.Length);
            return Content("");
        }

        public async Task<IActionResult> Return()
        {
            var processor = await _paymentPluginManager.LoadPluginBySystemNameAsync("Payments.AliPay") as AliPayPaymentProcessor;

            if (processor == null || !_paymentPluginManager.IsPluginActive(processor) || !processor.PluginDescriptor.Installed)
                throw new NopException("AliPay module cannot be loaded");

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        #endregion
    }
}