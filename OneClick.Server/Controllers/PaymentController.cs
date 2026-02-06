using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneClick.Server.Data;
using OneClick.Server.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OneClick.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentController> _logger;

    // TEST Secret Key (Get from Toss Developers)
    private const string TossSecretKey = "test_sk_Z60kL2q_024K7k1p9b88rYow54eW";

    public PaymentController(AppDbContext context, IHttpClientFactory httpClientFactory, ILogger<PaymentController> logger)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    [HttpPost("confirm")]
    [Authorize] // Only logged-in users can confirm payment
    public async Task<IActionResult> ConfirmPayment([FromBody] PaymentConfirmRequestDto request)
    {
        // 1. Verify User
        var username = User.Identity?.Name;
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
        if (user == null) return Unauthorized();

        // 2. Call Toss Payments API to confirm
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.tosspayments.com/v1/payments/confirm");
            requestMessage.Content = content;
            
            // Basic Auth header with Secret Key
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TossSecretKey}:"));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Toss Payment Confirm Failed: {errorBody}");
                return BadRequest($"Payment confirmation failed: {response.ReasonPhrase}");
            }

            // 3. If successful, parsing orderId to determine subscription type
            // Format: {GUID}_{TYPE}_{CAT_ID} (e.g. "GUID_ALL_0" or "GUID_SINGLE_2")
            
            int? categoryId = null;
            if (request.orderId.Contains("_"))
            {
                var parts = request.orderId.Split('_');
                // parts[0] is Guid, parts[1] is Type, parts[2] is CatId
                if (parts.Length >= 3)
                {
                    var type = parts[1];
                    var catIdStr = parts[2];

                    if (type == "SINGLE" && int.TryParse(catIdStr, out int parsedId) && parsedId > 0)
                    {
                        categoryId = parsedId;
                    }
                    // If type == "ALL", categoryId remains null
                }
            }

            // Check if user already has an active subscription of the SAME type
            // For now, we just add a new record. (Enhancement: Extend existing)
            
            var subscription = new Subscription
            {
                UserId = user.Id,
                CategoryId = categoryId, // null for ALL, specific ID for SINGLE
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            
            string planName = categoryId.HasValue ? "단일 카테고리" : "올인원 무제한";
            _logger.LogInformation($"Subscription activated: {planName} for User {username}");

            return Ok(new { message = "Payment confirmed and subscription activated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment");
            return StatusCode(500, "Internal server error during payment confirmation.");
        }
    }

    public class PaymentConfirmRequestDto
    {
        public string paymentKey { get; set; } = string.Empty;
        public string orderId { get; set; } = string.Empty;
        public int amount { get; set; }
    }
}
