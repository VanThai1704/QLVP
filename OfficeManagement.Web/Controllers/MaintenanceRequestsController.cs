using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;
using OfficeManagement.Web.Models.ViewModels;

namespace OfficeManagement.Web.Controllers;

[AuthorizeRole(AppRoles.Manager, AppRoles.Admin, AppRoles.Tenant, AppRoles.Technician, AppRoles.Accountant)]
public class MaintenanceRequestsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        IQueryable<MaintenanceRequest> query = context.MaintenanceRequests
            .Include(m => m.Office)
            .Include(m => m.Tenant)
            .Include(m => m.AssignedEmployee);

        if (role == AppRoles.Tenant)
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            query = query.Where(m => m.TenantId == tenantId);
        }
        else if (role == AppRoles.Technician)
        {
            var employeeId = int.Parse(User.FindFirstValue("EmployeeId")!);
            query = query.Where(m => m.AssignedEmployeeId == employeeId);
        }

        var requests = await query.OrderByDescending(m => m.CreatedDate).ToListAsync();
        return View(requests);
    }

    [AuthorizeRole(AppRoles.Tenant)]
    public async Task<IActionResult> Create()
    {
        var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
        var offices = await GetTenantOfficesAsync(tenantId);

        ViewBag.Offices = new SelectList(offices, "Id", "Name");
        ViewBag.HasActiveContract = offices.Count > 0;
        return View(new MaintenanceRequest { CreatedDate = DateTime.Today, Priority = MaintenancePriorities.Normal });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Tenant)]
    public async Task<IActionResult> Create(MaintenanceRequest model)
    {
        var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
        var offices = await GetTenantOfficesAsync(tenantId);

        ViewBag.Offices = new SelectList(offices, "Id", "Name", model.OfficeId);
        ViewBag.HasActiveContract = offices.Count > 0;

        model.TenantId = tenantId;
        model.Status = MaintenanceStatuses.Pending;
        model.RequestCode = string.IsNullOrWhiteSpace(model.RequestCode)
            ? await GenerateRequestCodeAsync()
            : model.RequestCode.Trim();

        ModelState.Remove(nameof(model.RequestCode));
        ModelState.Remove(nameof(model.Status));
        ModelState.Remove(nameof(model.TenantId));
        ModelState.Remove(nameof(model.Id));
        ModelState.Remove(nameof(model.AssignedEmployeeId));
        ModelState.Remove(nameof(model.CompletedDate));
        ModelState.Remove(nameof(model.Office));
        ModelState.Remove(nameof(model.Tenant));
        ModelState.Remove("Office");
        ModelState.Remove("Tenant");

        if (model.OfficeId <= 0)
            ModelState.AddModelError(nameof(model.OfficeId), "Vui lòng chọn văn phòng.");

        if (!ModelState.IsValid)
            return View(model);

        var createdDate = model.CreatedDate.Date;
        var hasActiveContract = await context.Contracts.AnyAsync(c =>
            c.TenantId == tenantId &&
            c.OfficeId == model.OfficeId &&
            c.Status == ContractStatuses.Active &&
            createdDate >= c.StartDate.Date &&
            createdDate <= c.EndDate.Date);

        if (!hasActiveContract)
        {
            ModelState.AddModelError(nameof(model.OfficeId), "Bạn không có hợp đồng hiệu lực với văn phòng này.");
            return View(model);
        }

        model.CreatedDate = createdDate;

        var request = new MaintenanceRequest
        {
            RequestCode = model.RequestCode,
            OfficeId = model.OfficeId,
            TenantId = model.TenantId,
            Description = model.Description,
            Priority = model.Priority,
            Status = model.Status,
            CreatedDate = model.CreatedDate
        };

        context.MaintenanceRequests.Add(request);
        await context.SaveChangesAsync();

        TempData["Success"] = "Đã gửi yêu cầu sửa chữa.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Assign(int id)
    {
        var request = await context.MaintenanceRequests
            .Include(m => m.Office)
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (request is null) return NotFound();

        ViewBag.Technicians = new SelectList(
            await context.Employees
                .Where(e => e.Position == "Technician")
                .OrderBy(e => e.FullName)
                .ToListAsync(),
            "Id", "FullName", request.AssignedEmployeeId);

        return View(new AssignTechnicianViewModel
        {
            RequestId = request.Id,
            RequestCode = request.RequestCode,
            AssignedEmployeeId = request.AssignedEmployeeId ?? 0
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Assign(AssignTechnicianViewModel model)
    {
        var request = await context.MaintenanceRequests.FindAsync(model.RequestId);
        if (request is null) return NotFound();

        request.AssignedEmployeeId = model.AssignedEmployeeId;
        if (request.Status == MaintenanceStatuses.Pending)
            request.Status = MaintenanceStatuses.InProgress;

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã phân công kỹ thuật viên.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(AppRoles.Technician, AppRoles.Admin)]
    public async Task<IActionResult> UpdateProgress(int id)
    {
        var employeeId = User.FindFirstValue("EmployeeId");
        var request = await context.MaintenanceRequests
            .Include(m => m.Office)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (request is null) return NotFound();

        if (User.IsInRole(AppRoles.Technician) &&
            request.AssignedEmployeeId?.ToString() != employeeId)
            return RedirectToAction("AccessDenied", "Account");

        return View(new UpdateRepairViewModel
        {
            Id = request.Id,
            RequestCode = request.RequestCode,
            Description = request.Description,
            Status = request.Status,
            CompletedDate = request.CompletedDate
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Technician, AppRoles.Admin)]
    public async Task<IActionResult> UpdateProgress(UpdateRepairViewModel model)
    {
        var request = await context.MaintenanceRequests
            .Include(m => m.Office)
            .FirstOrDefaultAsync(m => m.Id == model.Id);

        if (request is null) return NotFound();

        if (User.IsInRole(AppRoles.Technician) &&
            request.AssignedEmployeeId?.ToString() != User.FindFirstValue("EmployeeId"))
            return RedirectToAction("AccessDenied", "Account");

        request.Status = model.Status;
        request.CompletedDate = model.Status == MaintenanceStatuses.Completed
            ? model.CompletedDate ?? DateTime.Today
            : null;

        if (model.Status == MaintenanceStatuses.InProgress &&
            request.Office?.Status == OfficeStatuses.Rented)
        {
            // optional: mark office maintenance if critical - skip for simplicity
        }

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật tiến độ sửa chữa.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await context.MaintenanceRequests
            .Include(m => m.Office)
            .Include(m => m.Tenant)
            .Include(m => m.AssignedEmployee)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (request is null) return NotFound();

        if (User.IsInRole(AppRoles.Tenant))
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            if (request.TenantId != tenantId)
                return RedirectToAction("AccessDenied", "Account");
        }

        return View(request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Tenant, AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Cancel(int id)
    {
        var request = await context.MaintenanceRequests.FindAsync(id);
        if (request is null) return NotFound();

        if (User.IsInRole(AppRoles.Tenant))
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            if (request.TenantId != tenantId)
                return RedirectToAction("AccessDenied", "Account");
        }

        if (request.Status == MaintenanceStatuses.Completed)
        {
            TempData["Error"] = "Không thể hủy yêu cầu đã hoàn thành.";
            return RedirectToAction(nameof(Details), new { id });
        }

        request.Status = MaintenanceStatuses.Cancelled;
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã hủy yêu cầu sửa chữa.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateRequestCodeAsync()
    {
        var existingCodes = await context.MaintenanceRequests
            .Select(m => m.RequestCode)
            .ToListAsync();

        var max = 0;
        foreach (var code in existingCodes)
        {
            if (code.StartsWith("MR-", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(code[3..], out var number))
                max = Math.Max(max, number);
        }

        return $"MR-{max + 1:D3}";
    }

    private async Task<List<Office>> GetTenantOfficesAsync(int tenantId) =>
        await context.Contracts
            .Where(c => c.TenantId == tenantId && c.Status == ContractStatuses.Active)
            .Select(c => c.OfficeId)
            .Distinct()
            .Join(
                context.Offices,
                officeId => officeId,
                office => office.Id,
                (_, office) => office)
            .OrderBy(o => o.Name)
            .ToListAsync();
}
