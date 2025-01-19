using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EventService.Application.Interfaces.Services.Payments;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _merchantId;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(HttpClient httpClient, string merchantId, ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _merchantId = merchantId;
        _logger = logger;
    }

    public async Task<string?> RequestPaymentAsync(decimal amount, string callbackUrl, string description)
    {
        var request = new
        {
            merchant_id = _merchantId,
            amount = (int)amount * 10, // Zarinpal uses Rials, not Tomans
            callback_url = callbackUrl,
            description = description
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.zarinpal.com/pg/v4/payment/request.json", request);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data) && data.TryGetProperty("authority", out var authority))
        {
            return authority.GetString(); // ✅ Payment authority to redirect user
        }

        _logger.LogError("Payment request failed: {Response}", jsonResponse);
        return null;
    }

    public async Task<bool> VerifyPaymentAsync(string authority, decimal amount)
    {
        var request = new
        {
            merchant_id = _merchantId,
            amount = (int)amount * 10,
            authority = authority
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.zarinpal.com/pg/v4/payment/verify.json", request);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data) && data.TryGetProperty("code", out var code) && code.GetInt32() == 100)
        {
            return true; // ✅ Payment successful
        }

        _logger.LogError("Payment verification failed: {Response}", jsonResponse);
        return false;
    }
}
