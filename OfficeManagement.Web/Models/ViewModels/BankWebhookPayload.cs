using System.Text.Json.Serialization;

namespace OfficeManagement.Web.Models.ViewModels;

/// <summary>
/// Payload tương thích webhook SePay / Casso (giao dịch tiền vào).
/// </summary>
public class BankWebhookPayload
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; set; }

    [JsonPropertyName("transferType")]
    public string? TransferType { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }
}

public class PaymentStatusResponse
{
    public string Status { get; set; } = "Pending";
    public string? Message { get; set; }
    public string? RedirectUrl { get; set; }
}
