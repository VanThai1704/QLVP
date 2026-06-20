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

[AuthorizeRole(AppRoles.Manager, AppRoles.Admin, AppRoles.Tenant)]
public class ContractsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        await BusinessRulesService.RefreshContractStatusesAsync(context);

        var role = User.FindFirstValue(ClaimTypes.Role);
        IQueryable<Contract> query = context.Contracts
            .Include(c => c.Tenant)
            .Include(c => c.Office)
            .Include(c => c.CreatedByEmployee);

        if (role == AppRoles.Tenant)
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            query = query.Where(c => c.TenantId == tenantId);
        }

        var contracts = await query.OrderByDescending(c => c.StartDate).ToListAsync();
        return View(contracts);
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Create(int? officeId = null, int? tenantId = null, string? startDate = null, string? endDate = null, int? requestId = null)
    {
        await LoadCreateListsAsync(officeId);

        var model = new ContractCreateViewModel();

        if (officeId.HasValue)
            model.OfficeId = officeId.Value;
        if (tenantId.HasValue)
            model.TenantId = tenantId.Value;
        if (DateTime.TryParse(startDate, out var sd))
        {
            model.StartDate = sd;
            model.SignedDate = sd;
        }
        if (DateTime.TryParse(endDate, out var ed))
            model.EndDate = ed;

        if (officeId.HasValue)
        {
            var office = await context.Offices.FindAsync(officeId.Value);
            if (office != null)
                model.MonthlyRent = office.MonthlyRent;
        }

        ViewBag.RentalRequestId = requestId;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Create(ContractCreateViewModel model, int? requestId = null)
    {
        await LoadCreateListsAsync(model.OfficeId);

        if (!ModelState.IsValid)
        {
            ViewBag.RentalRequestId = requestId;
            return View(model);
        }

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            ViewBag.RentalRequestId = requestId;
            return View(model);
        }

        var office = await context.Offices.FindAsync(model.OfficeId);
        if (office is null)
        {
            ModelState.AddModelError(nameof(model.OfficeId), "Không tìm thấy văn phòng.");
            ViewBag.RentalRequestId = requestId;
            return View(model);
        }

        if (office.Status == OfficeStatuses.Maintenance)
        {
            ModelState.AddModelError(nameof(model.OfficeId), "Văn phòng đang bảo trì, không thể cho thuê.");
            ViewBag.RentalRequestId = requestId;
            return View(model);
        }

        var hasOverlap = await context.Contracts.AnyAsync(c =>
            c.OfficeId == model.OfficeId &&
            c.Status == ContractStatuses.Active &&
            model.StartDate <= c.EndDate &&
            model.EndDate >= c.StartDate);

        if (hasOverlap)
        {
            ModelState.AddModelError(nameof(model.OfficeId), "Văn phòng đã có hợp đồng trùng thời gian.");
            ViewBag.RentalRequestId = requestId;
            return View(model);
        }

        int? employeeId = null;
        var empClaim = User.FindFirstValue("EmployeeId");
        if (empClaim != null)
            employeeId = int.Parse(empClaim);

        var contract = new Contract
        {
            ContractCode = model.ContractCode,
            SignedDate = model.SignedDate,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            DepositAmount = model.DepositAmount,
            MonthlyRent = model.MonthlyRent > 0 ? model.MonthlyRent : office.MonthlyRent,
            Terms = model.Terms,
            Status = ContractStatuses.Active,
            TenantId = model.TenantId,
            OfficeId = model.OfficeId,
            CreatedByEmployeeId = employeeId
        };

        context.Contracts.Add(contract);

        foreach (var serviceTypeId in model.SelectedServiceTypeIds.Distinct())
        {
            var serviceType = await context.ServiceTypes.FindAsync(serviceTypeId);
            if (serviceType is null) continue;

            var exists = await context.OfficeServices
                .AnyAsync(os => os.OfficeId == model.OfficeId && os.ServiceTypeId == serviceTypeId);

            if (!exists)
            {
                context.OfficeServices.Add(new OfficeService
                {
                    OfficeId = model.OfficeId,
                    ServiceTypeId = serviceTypeId,
                    UnitPrice = serviceType.DefaultUnitPrice,
                    IsActive = true
                });
            }
        }

        office.Status = OfficeStatuses.Rented;

        // Approve the associated rental request if present
        if (requestId.HasValue)
        {
            var rentalRequest = await context.RentalRequests.FindAsync(requestId.Value);
            if (rentalRequest != null && rentalRequest.Status == RentalRequestStatuses.Pending)
            {
                rentalRequest.Status = RentalRequestStatuses.Approved;
            }
        }

        // Auto-reject all other pending requests for the same office
        var otherPending = await context.RentalRequests
            .Where(r => r.OfficeId == model.OfficeId && r.Status == RentalRequestStatuses.Pending)
            .ToListAsync();
        foreach (var r in otherPending)
        {
            if (requestId.HasValue && r.Id == requestId.Value)
                continue;
            r.Status = RentalRequestStatuses.Rejected;
        }

        await context.SaveChangesAsync();

        TempData["Success"] = "Đã tạo hợp đồng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Extend(int id)
    {
        var contract = await context.Contracts
            .Include(c => c.Tenant)
            .Include(c => c.Office)
            .FirstOrDefaultAsync(c => c.Id == id);
        return contract is null ? NotFound() : View(contract);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Extend(int id, DateTime newEndDate)
    {
        var contract = await context.Contracts.FindAsync(id);
        if (contract is null) return NotFound();

        if (newEndDate <= contract.EndDate)
        {
            TempData["Error"] = "Ngày kết thúc mới phải sau ngày kết thúc hiện tại.";
            return RedirectToAction(nameof(Extend), new { id });
        }

        contract.EndDate = newEndDate;
        contract.Status = ContractStatuses.Active;
        await context.SaveChangesAsync();

        TempData["Success"] = "Đã gia hạn hợp đồng.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Terminate(int id)
    {
        var contract = await context.Contracts
            .Include(c => c.Tenant)
            .Include(c => c.Office)
            .FirstOrDefaultAsync(c => c.Id == id);
        return contract is null ? NotFound() : View(contract);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> TerminateConfirmed(int id)
    {
        var contract = await context.Contracts
            .Include(c => c.Office)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null) return NotFound();

        contract.Status = ContractStatuses.Terminated;

        var hasOtherActive = await context.Contracts.AnyAsync(c =>
            c.OfficeId == contract.OfficeId &&
            c.Id != contract.Id &&
            c.Status == ContractStatuses.Active);

        if (!hasOtherActive && contract.Office is not null)
            contract.Office.Status = OfficeStatuses.Available;

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã kết thúc hợp đồng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var contract = await context.Contracts
            .Include(c => c.Tenant)
            .Include(c => c.Office)
            .Include(c => c.CreatedByEmployee)
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null) return NotFound();

        if (User.IsInRole(AppRoles.Tenant))
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            if (contract.TenantId != tenantId)
                return RedirectToAction("AccessDenied", "Account");
        }

        return View(contract);
    }

    [HttpGet]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> GetOfficeRent(int officeId)
    {
        var office = await context.Offices.FindAsync(officeId);
        return office is null ? NotFound() : Json(new { monthlyRent = office.MonthlyRent });
    }

    private async Task LoadCreateListsAsync(int? officeId = null)
    {
        ViewBag.Tenants = new SelectList(await context.Tenants.OrderBy(t => t.CompanyName).ToListAsync(), "Id", "CompanyName");
        ViewBag.Offices = new SelectList(
            await context.Offices
                .Where(o => o.Status == OfficeStatuses.Available || o.Id == officeId)
                .OrderBy(o => o.RoomNumber)
                .ToListAsync(),
            "Id", "DisplayLabel");

        ViewBag.ServiceTypes = await context.ServiceTypes.OrderBy(s => s.Name).ToListAsync();
    }
}
