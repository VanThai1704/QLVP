using Microsoft.Extensions.Options;
using OfficeManagement.Web.Models.Entities;
using OfficeManagement.Web.Models.Settings;
using OfficeManagement.Web.Models.ViewModels;
using QRCoder;

namespace OfficeManagement.Web.Services;

public class BankTransferService(IOptions<BankTransferSettings> options)
{
    private readonly BankTransferSettings _settings = options.Value;

    public BankTransferViewModel CreateViewModel(Invoice invoice)
    {
        var transferContent = BuildTransferContent(invoice.InvoiceCode);
        var amount = (long)Math.Round(invoice.TotalAmount, 0);

        var vm = new BankTransferViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceCode = invoice.InvoiceCode,
            TotalAmount = invoice.TotalAmount,
            BankName = _settings.BankName,
            AccountNumber = _settings.AccountNumber,
            AccountName = _settings.AccountName,
            Branch = _settings.Branch,
            TransferContent = transferContent
        };

        if (!string.IsNullOrWhiteSpace(_settings.BankId) && !string.IsNullOrWhiteSpace(_settings.AccountNumber))
        {
            vm.QrCodeImageUrl =
                $"https://img.vietqr.io/image/{_settings.BankId}-{_settings.AccountNumber}-{_settings.QrTemplate}.png" +
                $"?amount={amount}&addInfo={Uri.EscapeDataString(transferContent)}&accountName={Uri.EscapeDataString(_settings.AccountName)}";
        }

        vm.QrCodeDataUri = GenerateQrDataUri(BuildQrText(vm));
        return vm;
    }

    private static string BuildTransferContent(string invoiceCode) =>
        BankPaymentService.BuildPaymentReference(invoiceCode);

    private static string BuildQrText(BankTransferViewModel vm) =>
        $"NGAN HANG: {vm.BankName}\n" +
        $"SO TK: {vm.AccountNumber}\n" +
        $"CHU TK: {vm.AccountName}\n" +
        $"SO TIEN: {vm.TotalAmount:N0} VND\n" +
        $"NOI DUNG: {vm.TransferContent}";

    private static string GenerateQrDataUri(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(8);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
