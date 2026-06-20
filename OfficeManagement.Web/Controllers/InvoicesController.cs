using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;
using OfficeManagement.Web.Models.ViewModels;
using OfficeManagement.Web.Services;

namespace OfficeManagement.Web.Controllers;

[AuthorizeRole(AppRoles.Accountant, AppRoles.Admin, AppRoles.Manager, AppRoles.Tenant)]
public class InvoicesController(
    ApplicationDbContext context,
    BankTransferService bankTransferService,
    BankPaymentService bankPaymentService) : Controller
{
    public async Task<IActionResult> Index(string? status, string? tenantName)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        IQueryable<Invoice> query = context.Invoices
            .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
            .Include(i => i.Contract).ThenInclude(c => c!.Office);

        if (role == AppRoles.Tenant)
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            query = query.Where(i => i.Contract!.TenantId == tenantId);
        }
        else if (!string.IsNullOrWhiteSpace(tenantName))
        {
            var term = tenantName.Trim();
            query = query.Where(i =>
                i.Contract!.Tenant!.CompanyName.Contains(term) ||
                i.Contract!.Tenant!.RepresentativeName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) && InvoiceStatuses.All.Contains(status))
            query = query.Where(i => i.Status == status);

        var invoices = await query.OrderByDescending(i => i.BillingYear)
            .ThenByDescending(i => i.BillingMonth)
            .ToListAsync();

        return View(new InvoiceIndexViewModel
        {
            Status = status,
            TenantName = tenantName,
            Invoices = invoices
        });
    }

    [AuthorizeRole(AppRoles.Accountant, AppRoles.Admin, AppRoles.Manager)]
    public async Task<IActionResult> Create()
    {
        await LoadInvoiceListsAsync();
        return View(new InvoiceCreateViewModel
        {
            InvoiceCode = await GenerateInvoiceCodeAsync(),
            BillingMonth = (byte)DateTime.Today.Month,
            BillingYear = (short)DateTime.Today.Year,
            IssueDate = DateTime.Today
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Accountant, AppRoles.Admin, AppRoles.Manager)]
    public async Task<IActionResult> Create(InvoiceCreateViewModel model)
    {
        await LoadInvoiceListsAsync(model.ContractId);

        if (!ModelState.IsValid) return View(model);

        var contract = await context.Contracts
            .Include(c => c.Office)
            .FirstOrDefaultAsync(c => c.Id == model.ContractId);

        if (contract is null)
        {
            ModelState.AddModelError(nameof(model.ContractId), "Không tìm thấy hợp đồng.");
            return View(model);
        }

        if (!contract.IsActiveOn(model.IssueDate))
        {
            ModelState.AddModelError(nameof(model.ContractId), "Hợp đồng không còn hiệu lực tại ngày lập.");
            return View(model);
        }

        var duplicate = await context.Invoices.AnyAsync(i =>
            i.ContractId == model.ContractId &&
            i.BillingMonth == model.BillingMonth &&
            i.BillingYear == model.BillingYear);

        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, "Đã tồn tại hóa đơn cho kỳ này.");
            return View(model);
        }

        var invoiceCode = string.IsNullOrWhiteSpace(model.InvoiceCode)
            ? await GenerateInvoiceCodeAsync()
            : model.InvoiceCode.Trim();

        if (await context.Invoices.AnyAsync(i => i.InvoiceCode == invoiceCode))
        {
            invoiceCode = await GenerateInvoiceCodeAsync();
        }

        var invoice = new Invoice
        {
            InvoiceCode = invoiceCode,
            ContractId = model.ContractId,
            BillingMonth = model.BillingMonth,
            BillingYear = model.BillingYear,
            IssueDate = model.IssueDate,
            RentAmount = contract.MonthlyRent,
            Status = InvoiceStatuses.Unpaid
        };

        foreach (var line in model.Lines.Where(l => l.CurrentReading >= l.PreviousReading))
        {
            invoice.Details.Add(new InvoiceDetail
            {
                OfficeServiceId = line.OfficeServiceId,
                PreviousReading = line.PreviousReading,
                CurrentReading = line.CurrentReading,
                UnitPrice = line.UnitPrice
            });
        }

        invoice.RecalculateTotals();
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        TempData["Success"] = "Đã lập hóa đơn hàng tháng.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var invoice = await context.Invoices
            .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
            .Include(i => i.Contract).ThenInclude(c => c!.Office)
            .Include(i => i.Details).ThenInclude(d => d.OfficeService).ThenInclude(os => os!.ServiceType)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice is null) return NotFound();

        if (User.IsInRole(AppRoles.Tenant))
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            if (invoice.Contract!.TenantId != tenantId)
                return RedirectToAction("AccessDenied", "Account");
        }

        return View(invoice);
    }

    [AuthorizeRole(AppRoles.Accountant, AppRoles.Admin, AppRoles.Manager)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(int id)
    {
        var invoice = await context.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();
        if (invoice.Status == InvoiceStatuses.Paid)
        {
            TempData["Error"] = "Hóa đơn đã được thanh toán.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var wasPendingCash = invoice.Status == InvoiceStatuses.PendingPayment;

        invoice.Status = InvoiceStatuses.Paid;
        invoice.PaidDate = DateTime.Today;
        if (string.IsNullOrEmpty(invoice.PaymentMethod))
            invoice.PaymentMethod = PaymentMethods.BankTransfer;
        await context.SaveChangesAsync();

        TempData["Success"] = wasPendingCash
            ? "Đã ghi nhận thanh toán tiền mặt tại lễ tân."
            : "Đã ghi nhận thanh toán.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [AuthorizeRole(AppRoles.Tenant)]
    public async Task<IActionResult> Pay(int id)
    {
        var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
        var invoice = await context.Invoices
            .Include(i => i.Contract)
            .FirstOrDefaultAsync(i => i.Id == id && i.Contract!.TenantId == tenantId);

        if (invoice is null) return NotFound();
        if (invoice.Status == InvoiceStatuses.Paid)
        {
            TempData["Error"] = "Hóa đơn đã được thanh toán.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (invoice.Status == InvoiceStatuses.PendingPayment)
        {
            TempData["Success"] = "Hóa đơn đang chờ thanh toán tại lễ tân. Vui lòng đến quầy lễ tân để hoàn tất.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new PaymentViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceCode = invoice.InvoiceCode,
            TotalAmount = invoice.TotalAmount
        });
    }

    [AuthorizeRole(AppRoles.Tenant)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(PaymentViewModel model)
    {
        var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
        var invoice = await context.Invoices
            .Include(i => i.Contract)
            .FirstOrDefaultAsync(i => i.Id == model.InvoiceId && i.Contract!.TenantId == tenantId);

        if (invoice is null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        if (model.PaymentMethod == PaymentMethods.BankTransfer)
            return RedirectToAction(nameof(BankTransfer), new { id = invoice.Id });

        if (model.PaymentMethod == PaymentMethods.Cash)
        {
            invoice.Status = InvoiceStatuses.PendingPayment;
            invoice.PaymentMethod = PaymentMethods.Cash;
            invoice.PaidDate = null;
            await context.SaveChangesAsync();

            TempData["Success"] = "Vui lòng đến quầy lễ tân để hoàn tất thanh toán tiền mặt. Hóa đơn đã chuyển sang trạng thái «Đang chờ thanh toán».";
            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }

        invoice.Status = InvoiceStatuses.Paid;
        invoice.PaidDate = DateTime.Today;
        invoice.PaymentMethod = model.PaymentMethod;
        await context.SaveChangesAsync();

        TempData["Success"] = "Thanh toán thành công.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    [AuthorizeRole(AppRoles.Tenant)]
    public async Task<IActionResult> BankTransfer(int id)
    {
        var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
        var invoice = await context.Invoices
            .Include(i => i.Contract)
            .FirstOrDefaultAsync(i => i.Id == id && i.Contract!.TenantId == tenantId);

        if (invoice is null) return NotFound();
        if (invoice.Status == InvoiceStatuses.Paid)
        {
            TempData["Error"] = "Hóa đơn đã được thanh toán.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await bankPaymentService.StartPendingPaymentAsync(invoice);
        return View(bankTransferService.CreateViewModel(invoice));
    }

    [AuthorizeRole(AppRoles.Tenant)]
    [HttpGet]
    public async Task<IActionResult> PaymentStatus(int id)
    {
        var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
        var result = await bankPaymentService.CheckPaymentStatusAsync(id, tenantId);
        return Json(result);
    }

    [AuthorizeRole(AppRoles.Tenant)]
    [HttpPost]
    public async Task<IActionResult> SimulatePayment(int id)
    {
        var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
        var invoice = await context.Invoices
            .Include(i => i.Contract)
            .FirstOrDefaultAsync(i => i.Id == id && i.Contract!.TenantId == tenantId);

        if (invoice is null) return NotFound();
        if (invoice.Status == InvoiceStatuses.Paid) return BadRequest("Hóa đơn đã được thanh toán.");

        var reference = BankPaymentService.BuildPaymentReference(invoice.InvoiceCode);
        var confirmed = await bankPaymentService.TryConfirmPaymentAsync(reference, invoice.TotalAmount, "SIM-" + Guid.NewGuid().ToString("N")[..8].ToUpper());

        if (confirmed)
        {
            return Json(new { success = true, message = "Mô phỏng chuyển khoản thành công! Hệ thống đang xử lý..." });
        }
        return Json(new { success = false, message = "Không thể mô phỏng chuyển khoản. Vui lòng kiểm tra lại." });
    }

    [AuthorizeRole(AppRoles.Accountant, AppRoles.Admin, AppRoles.Manager)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var invoice = await context.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();
        if (invoice.Status == InvoiceStatuses.Paid)
        {
            TempData["Error"] = "Không thể hủy hóa đơn đã thanh toán.";
            return RedirectToAction(nameof(Details), new { id });
        }

        invoice.Status = InvoiceStatuses.Cancelled;
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã hủy hóa đơn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [AuthorizeRole(AppRoles.Accountant, AppRoles.Admin, AppRoles.Manager)]
    public async Task<IActionResult> Revenue()
    {
        var revenueGroups = await context.Invoices
            .Where(i => i.Status == InvoiceStatuses.Paid && i.PaidDate != null)
            .GroupBy(i => new { i.PaidDate!.Value.Year, i.PaidDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync();

        var stats = revenueGroups
            .Select(x => new RevenueStatItem
            {
                Period = $"{x.Month:00}/{x.Year}",
                Amount = x.Amount
            })
            .ToList();

        ViewBag.TotalRevenue = stats.Sum(s => s.Amount);
        return View(stats);
    }

    [HttpGet]
    [AuthorizeRole(AppRoles.Accountant, AppRoles.Admin, AppRoles.Manager)]
    public async Task<IActionResult> LoadServiceLines(int contractId)
    {
        var contract = await context.Contracts.FindAsync(contractId);
        if (contract is null) return NotFound();

        var officeServices = await context.OfficeServices
            .Include(os => os.ServiceType)
            .Where(os => os.OfficeId == contract.OfficeId && os.IsActive)
            .ToListAsync();

        var lines = new List<InvoiceLineInput>();
        foreach (var os in officeServices)
        {
            decimal prevReading = 0;
            if (os.ServiceType!.IsMetered)
            {
                var lastDetail = await context.InvoiceDetails
                    .Include(d => d.Invoice)
                    .Where(d => d.OfficeServiceId == os.Id && d.Invoice!.ContractId == contract.Id && d.Invoice.Status != InvoiceStatuses.Cancelled)
                    .OrderByDescending(d => d.Invoice!.BillingYear)
                    .ThenByDescending(d => d.Invoice!.BillingMonth)
                    .FirstOrDefaultAsync();

                if (lastDetail is not null)
                {
                    prevReading = lastDetail.CurrentReading;
                }
            }

            lines.Add(new InvoiceLineInput
            {
                OfficeServiceId = os.Id,
                ServiceName = os.ServiceType.Name,
                IsMetered = os.ServiceType.IsMetered,
                UnitPrice = os.UnitPrice,
                PreviousReading = prevReading,
                CurrentReading = os.ServiceType.IsMetered ? prevReading : 1
            });
        }

        return PartialView("_InvoiceLines", lines);
    }

    private async Task LoadInvoiceListsAsync(int? contractId = null)
    {
        ViewBag.Contracts = new SelectList(
            await context.Contracts
                .Include(c => c.Tenant)
                .Include(c => c.Office)
                .Where(c => c.Status == ContractStatuses.Active)
                .OrderBy(c => c.ContractCode)
                .Select(c => new
                {
                    c.Id,
                    Display = c.ContractCode + " - " + c.Tenant!.CompanyName + " (" + c.Office!.Name + ")"
                })
                .ToListAsync(),
            "Id", "Display", contractId);

        if (contractId.HasValue)
        {
            var contract = await context.Contracts.FindAsync(contractId.Value);
            if (contract is not null)
            {
                var officeServices = await context.OfficeServices
                    .Include(os => os.ServiceType)
                    .Where(os => os.OfficeId == contract.OfficeId && os.IsActive)
                    .ToListAsync();

                var lines = new List<InvoiceLineInput>();
                foreach (var os in officeServices)
                {
                    decimal prevReading = 0;
                    if (os.ServiceType!.IsMetered)
                    {
                        var lastDetail = await context.InvoiceDetails
                            .Include(d => d.Invoice)
                            .Where(d => d.OfficeServiceId == os.Id && d.Invoice!.ContractId == contract.Id && d.Invoice.Status != InvoiceStatuses.Cancelled)
                            .OrderByDescending(d => d.Invoice!.BillingYear)
                            .ThenByDescending(d => d.Invoice!.BillingMonth)
                            .FirstOrDefaultAsync();

                        if (lastDetail is not null)
                        {
                            prevReading = lastDetail.CurrentReading;
                        }
                    }

                    lines.Add(new InvoiceLineInput
                    {
                        OfficeServiceId = os.Id,
                        ServiceName = os.ServiceType.Name,
                        IsMetered = os.ServiceType.IsMetered,
                        UnitPrice = os.UnitPrice,
                        PreviousReading = prevReading,
                        CurrentReading = os.ServiceType.IsMetered ? prevReading : 1
                    });
                }
                ViewBag.Lines = lines;
            }
        }
    }

    private async Task<string> GenerateInvoiceCodeAsync()
    {
        var codes = await context.Invoices.Select(i => i.InvoiceCode).ToListAsync();
        var max = 0;
        foreach (var code in codes)
        {
            if (code.StartsWith("INV-", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(code[4..], out var number))
                max = Math.Max(max, number);
        }

        return $"INV-{max + 1:D3}";
    }
}
