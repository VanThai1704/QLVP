using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;
using OfficeManagement.Web.Models.Settings;
using OfficeManagement.Web.Models.ViewModels;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OfficeManagement.Web.Services;

public class BankPaymentService(
    ApplicationDbContext context,
    IOptions<BankTransferSettings> options,
    IHttpClientFactory httpClientFactory,
    ILogger<BankPaymentService> logger)
{
    private readonly BankTransferSettings _settings = options.Value;

    public static string BuildPaymentReference(string invoiceCode) => $"TT {invoiceCode}";

    public async Task<BankPaymentPending> StartPendingPaymentAsync(Invoice invoice)
    {
        var reference = BuildPaymentReference(invoice.InvoiceCode);
        var existing = await context.BankPaymentPending
            .Where(p => p.InvoiceId == invoice.Id && p.Status == BankPaymentStatuses.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing is not null && existing.ExpiresAt > DateTime.UtcNow)
            return existing;

        var pending = new BankPaymentPending
        {
            InvoiceId = invoice.Id,
            PaymentReference = reference,
            ExpectedAmount = invoice.TotalAmount,
            Status = BankPaymentStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.PaymentTimeoutMinutes)
        };

        context.BankPaymentPending.Add(pending);
        await context.SaveChangesAsync();
        return pending;
    }

    public async Task<PaymentStatusResponse> CheckPaymentStatusAsync(int invoiceId, int tenantId)
    {
        var invoice = await context.Invoices
            .Include(i => i.Contract)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Contract!.TenantId == tenantId);

        if (invoice is null)
            return new PaymentStatusResponse { Status = "NotFound", Message = "Không tìm thấy hóa đơn." };

        if (invoice.Status == InvoiceStatuses.Paid)
            return PaidResponse(invoiceId, "Hóa đơn đã được thanh toán.");

        var pending = await GetActivePendingAsync(invoiceId);
        if (pending is null)
            return new PaymentStatusResponse { Status = "Pending", Message = "Đang khởi tạo phiên thanh toán..." };

        if (pending.ExpiresAt <= DateTime.UtcNow)
        {
            pending.Status = BankPaymentStatuses.Expired;
            await context.SaveChangesAsync();
            return new PaymentStatusResponse { Status = "Expired", Message = "Phiên thanh toán đã hết hạn. Vui lòng thử lại." };
        }

        if (_settings.AutoPollingEnabled && !string.IsNullOrWhiteSpace(_settings.SePayApiKey))
            await TryPollSePayAsync(pending);

        await context.Entry(invoice).ReloadAsync();
        if (invoice.Status == InvoiceStatuses.Paid)
            return PaidResponse(invoiceId, "Thanh toán thành công! Hệ thống đã xác nhận giao dịch.");

        return new PaymentStatusResponse
        {
            Status = "Pending",
            Message = "Đang chờ xác nhận chuyển khoản từ ngân hàng..."
        };
    }

    public async Task<bool> ProcessWebhookAsync(BankWebhookPayload payload)
    {
        if (!IsIncomingTransfer(payload))
            return false;

        return await TryConfirmPaymentAsync(payload.Content ?? string.Empty, payload.TransferAmount, payload.Id);
    }

    public async Task<bool> TryConfirmPaymentAsync(string transferContent, decimal amount, string? externalId = null)
    {
        var normalizedContent = NormalizeContent(transferContent);
        if (string.IsNullOrWhiteSpace(normalizedContent))
            return false;

        var pendings = await context.BankPaymentPending
            .Include(p => p.Invoice)
            .Where(p => p.Status == BankPaymentStatuses.Pending && p.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var pending = pendings.FirstOrDefault(p =>
            ContentMatchesReference(normalizedContent, p.PaymentReference) &&
            AmountMatches(p.ExpectedAmount, amount));

        if (pending is null)
            return false;

        return await CompletePaymentAsync(pending, externalId);
    }

    private async Task TryPollSePayAsync(BankPaymentPending pending)
    {
        try
        {
            var transactions = await FetchSePayTransactionsAsync();
            foreach (var tx in transactions)
            {
                if (!tx.IsIncoming) continue;
                if (await TryConfirmPaymentAsync(tx.Content, tx.Amount, tx.Id))
                    return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SePay polling failed for invoice {InvoiceId}", pending.InvoiceId);
        }
    }

    private async Task<List<SePayTransaction>> FetchSePayTransactionsAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.SePayApiKey))
            return [];

        var client = httpClientFactory.CreateClient("SePay");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SePayApiKey);

        var response = await client.GetAsync("https://my.sepay.vn/userapi/transactions/list?limit=20");
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        if (!doc.RootElement.TryGetProperty("transactions", out var arr))
            return [];

        var list = new List<SePayTransaction>();
        foreach (var item in arr.EnumerateArray())
        {
            var content = item.TryGetProperty("transaction_content", out var c) ? c.GetString() ?? "" :
                item.TryGetProperty("content", out var c2) ? c2.GetString() ?? "" : "";

            var amount = item.TryGetProperty("amount_in", out var a) ? a.GetDecimal() :
                item.TryGetProperty("transferAmount", out var a2) ? a2.GetDecimal() : 0;

            var id = item.TryGetProperty("id", out var idEl) ? idEl.ToString() : null;
            var type = item.TryGetProperty("transferType", out var t) ? t.GetString() : "in";

            list.Add(new SePayTransaction
            {
                Id = id,
                Content = content,
                Amount = amount,
                IsIncoming = type is null or "in" or "credit"
            });
        }

        return list;
    }

    private async Task<bool> CompletePaymentAsync(BankPaymentPending pending, string? externalId)
    {
        var invoice = await context.Invoices.FindAsync(pending.InvoiceId);
        if (invoice is null || invoice.Status == InvoiceStatuses.Paid)
            return false;

        invoice.Status = InvoiceStatuses.Paid;
        invoice.PaidDate = DateTime.Today;
        invoice.PaymentMethod = PaymentMethods.BankTransfer;

        pending.Status = BankPaymentStatuses.Completed;
        pending.CompletedAt = DateTime.UtcNow;
        pending.ExternalTransactionId = externalId;

        await context.SaveChangesAsync();
        logger.LogInformation("Auto-confirmed bank payment for invoice {InvoiceCode}", invoice.InvoiceCode);
        return true;
    }

    private async Task<BankPaymentPending?> GetActivePendingAsync(int invoiceId) =>
        await context.BankPaymentPending
            .Where(p => p.InvoiceId == invoiceId && p.Status == BankPaymentStatuses.Pending && p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

    private static bool IsIncomingTransfer(BankWebhookPayload payload) =>
        payload.TransferType is null or "in" or "credit" && payload.TransferAmount > 0;

    private static bool ContentMatchesReference(string normalizedContent, string reference)
    {
        var normalizedRef = NormalizeContent(reference);
        return normalizedContent.Contains(normalizedRef, StringComparison.OrdinalIgnoreCase)
               || normalizedContent.Replace("-", "").Contains(normalizedRef.Replace("-", ""), StringComparison.OrdinalIgnoreCase);
    }

    private static bool AmountMatches(decimal expected, decimal received) =>
        Math.Abs(expected - received) < 1m;

    private static string NormalizeContent(string content) =>
        content.Trim().ToUpperInvariant();

    private PaymentStatusResponse PaidResponse(int invoiceId, string message) =>
        new()
        {
            Status = "Paid",
            Message = message,
            RedirectUrl = $"/Invoices/Details/{invoiceId}"
        };

    private sealed class SePayTransaction
    {
        public string? Id { get; init; }
        public string Content { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public bool IsIncoming { get; init; }
    }
}
