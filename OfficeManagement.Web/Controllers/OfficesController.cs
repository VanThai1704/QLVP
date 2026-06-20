using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;

namespace OfficeManagement.Web.Controllers;

public class OfficesController(ApplicationDbContext context) : Controller
{
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Index()
    {
        var offices = await context.Offices
            .OrderBy(o => o.RoomNumber)
            .ThenBy(o => o.OfficeCode)
            .ToListAsync();
        return View(offices);
    }

    [AuthorizeRole]
    public async Task<IActionResult> Browse(string? status)
    {
        var query = context.Offices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && OfficeStatuses.All.Contains(status))
            query = query.Where(o => o.Status == status);

        var offices = await query
            .OrderBy(o => o.Location)
            .ThenBy(o => o.RoomNumber)
            .ToListAsync();

        ViewBag.Status = status;
        return View(offices);
    }

    [AuthorizeRole]
    public async Task<IActionResult> Details(int id)
    {
        var office = await context.Offices
            .Include(o => o.OfficeServices).ThenInclude(os => os.ServiceType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (office is null) return NotFound();

        if (User.IsInRole(AppRoles.Tenant))
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (tenantIdClaim != null)
            {
                var tenantId = int.Parse(tenantIdClaim);
                ViewBag.HasPendingRequest = await context.RentalRequests.AnyAsync(r =>
                    r.OfficeId == id &&
                    r.TenantId == tenantId &&
                    r.Status == RentalRequestStatuses.Pending);
            }
        }

        return View(office);
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public IActionResult Create() => View(new Office { Status = OfficeStatuses.Available, Capacity = 10 });

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Create(Office office)
    {
        if (!ModelState.IsValid) return View(office);

        if (await context.Offices.AnyAsync(o => o.OfficeCode == office.OfficeCode))
        {
            ModelState.AddModelError(nameof(Office.OfficeCode), "Mã văn phòng đã tồn tại.");
            return View(office);
        }

        if (await context.Offices.AnyAsync(o => o.RoomNumber == office.RoomNumber))
        {
            ModelState.AddModelError(nameof(Office.RoomNumber), "Số phòng đã tồn tại.");
            return View(office);
        }

        context.Offices.Add(office);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã thêm văn phòng.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var office = await context.Offices.FindAsync(id);
        return office is null ? NotFound() : View(office);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, Office office)
    {
        if (id != office.Id) return NotFound();
        if (!ModelState.IsValid) return View(office);

        if (await context.Offices.AnyAsync(o => o.RoomNumber == office.RoomNumber && o.Id != office.Id))
        {
            ModelState.AddModelError(nameof(Office.RoomNumber), "Số phòng đã tồn tại.");
            return View(office);
        }

        var originalOffice = await context.Offices.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (originalOffice is null) return NotFound();

        context.Update(office);

        if ((office.Status == OfficeStatuses.Rented || office.Status == OfficeStatuses.Maintenance)
            && originalOffice.Status != office.Status)
        {
            var pendingRequests = await context.RentalRequests
                .Where(r => r.OfficeId == id && r.Status == RentalRequestStatuses.Pending)
                .ToListAsync();

            foreach (var req in pendingRequests)
            {
                req.Status = RentalRequestStatuses.Rejected;
            }
        }

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật văn phòng.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var office = await context.Offices.FindAsync(id);
        return office is null ? NotFound() : View(office);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var office = await context.Offices
            .Include(o => o.Contracts)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (office is null) return NotFound();

        if (office.Contracts.Any(c => c.Status == ContractStatuses.Active))
        {
            TempData["Error"] = "Không thể xóa văn phòng đang có hợp đồng hiệu lực.";
            return RedirectToAction(nameof(Index));
        }

        // Delete associated RentalRequests to avoid FK violations
        var requests = await context.RentalRequests.Where(r => r.OfficeId == id).ToListAsync();
        context.RentalRequests.RemoveRange(requests);

        context.Offices.Remove(office);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa văn phòng.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> StatusBoard()
    {
        var offices = await context.Offices
            .Include(o => o.Contracts.Where(c => c.Status == ContractStatuses.Active))
                .ThenInclude(c => c.Tenant)
            .OrderBy(o => o.Location)
            .ThenBy(o => o.RoomNumber)
            .ToListAsync();
        return View(offices);
    }
}
