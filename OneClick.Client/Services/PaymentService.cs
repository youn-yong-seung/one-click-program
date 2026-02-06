using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Net.Http;

namespace OneClick.Client.Services;

public class PaymentService
{
    private readonly IJSRuntime _jsRuntime;
    
    // TEST Client Key
    private const string ClientKey = "test_ck_D5GePWvyJnrK0W0k6q8gLzN97Eoq"; 

    private readonly HttpClient _http;

    public PaymentService(IJSRuntime jsRuntime, HttpClient http)
    {
        _jsRuntime = jsRuntime;
        _http = http;
    }

    // ... (Existing Methods)

    public async Task<string> RedeemCouponAsync(string code)
    {
        var response = await _http.PostAsJsonAsync("api/coupon/redeem", code);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(); // Success Message
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }
    }

    public async Task InitializePaymentWidgetAsync(string customerKey)
    {
        var key = string.IsNullOrEmpty(customerKey) ? "ANONYMOUS" : customerKey;
        await _jsRuntime.InvokeVoidAsync("paymentInterop.initPaymentWidget", ClientKey, key);
    }

    public async Task RenderPaymentMethodsAsync(string selector, int amount)
    {
        await _jsRuntime.InvokeVoidAsync("paymentInterop.renderPaymentMethod", selector, amount);
    }

    public async Task RenderPaymentMethodsWithCallbackAsync<T>(DotNetObjectReference<T> objRef, string selector, int amount) where T : class
    {
        await _jsRuntime.InvokeVoidAsync("paymentInterop.renderPaymentMethodWithCallback", objRef, selector, amount);
    }

    public async Task RenderAgreementAsync(string selector)
    {
        await _jsRuntime.InvokeVoidAsync("paymentInterop.renderAgreement", selector);
    }

    public async Task RequestPaymentAsync(string orderId, string orderName, int amount, string customerEmail, string customerName)
    {
        await _jsRuntime.InvokeVoidAsync("paymentInterop.requestPayment", 
            orderId, orderName, amount, customerEmail, customerName);
    }
}
