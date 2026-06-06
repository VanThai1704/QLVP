using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OfficeManagement.Web.Models.Settings;
using OfficeManagement.Web.Models.ViewModels;
using OfficeManagement.Web.Services;

namespace OfficeManagement.Web.Controllers;

[ApiController]
[Route("api/payments")]
public class BankWebhookController(
    BankPaymentService bankPaymentService,
    IOptions<BankTransferSettings> options,
    ILogger<BankWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Webhook nhận thông báo chuyển khoản (SePay, Casso hoặc test thủ công).
    /// Header: X-Webhook-Secret hoặc Authorization: Apikey {secret}
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] BankWebhookPayload payload)
    {
        if (!ValidateSecret())
        {
            logger.LogWarning("Bank webhook rejected: invalid secret");
            return Unauthorized();
        }

        var confirmed = await bankPaymentService.ProcessWebhookAsync(payload);
        return Ok(new { success = confirmed, message = confirmed ? "Payment confirmed" : "No matching payment" });
    }

    private bool ValidateSecret()
    {
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return true;

        if (Request.Headers.TryGetValue("X-Webhook-Secret", out var h) && h == secret)
            return true;

        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            var value = auth.ToString();
            if (value.Equals($"Apikey {secret}", StringComparison.OrdinalIgnoreCase) ||
                value.Equals($"Bearer {secret}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
