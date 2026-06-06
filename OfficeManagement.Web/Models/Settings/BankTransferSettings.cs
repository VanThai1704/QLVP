namespace OfficeManagement.Web.Models.Settings;

public class BankTransferSettings
{
    public string BankId { get; set; } = "970422";
    public string BankName { get; set; } = "Ngân hàng TMCP Quân đội (MB Bank)";
    public string AccountNumber { get; set; } = "0123456789";
    public string AccountName { get; set; } = "CONG TY QUAN LY VAN PHONG";
    public string Branch { get; set; } = "Chi nhánh Cần Thơ";
    public string QrTemplate { get; set; } = "compact2";

    /// <summary>API Key SePay để tự động đối soát giao dịch (https://sepay.vn).</summary>
    public string? SePayApiKey { get; set; }

    /// <summary>Mật khẩu webhook — gửi header X-Webhook-Secret khi test hoặc cấu hình trên SePay.</summary>
    public string WebhookSecret { get; set; } = "officepro-webhook-secret";

    /// <summary>Bật polling SePay khi khách đang ở trang QR.</summary>
    public bool AutoPollingEnabled { get; set; } = true;

    /// <summary>Thời gian chờ thanh toán (phút).</summary>
    public int PaymentTimeoutMinutes { get; set; } = 30;
}
