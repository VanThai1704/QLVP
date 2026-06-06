namespace OfficeManagement.Web.Models.Entities;

public class BankPaymentPending
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public string Status { get; set; } = BankPaymentStatuses.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ExternalTransactionId { get; set; }

    public Invoice Invoice { get; set; } = null!;
}

public static class BankPaymentStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Expired = "Expired";
}
