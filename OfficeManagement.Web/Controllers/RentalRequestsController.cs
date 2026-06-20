using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;

namespace OfficeManagement.Web.Controllers;

[AuthorizeRole]
public class RentalRequestsController(ApplicationDbContext context) : Controller
{
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Index()
    {
        var requests = await context.RentalRequests
            .Include(r => r.Office)
            .Include(r => r.Tenant)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [AuthorizeRole(AppRoles.Tenant)]
    public async Task<IActionResult> MyRequests()
    {
        var tenantIdClaim = User.FindFirstValue("TenantId");
        if (tenantIdClaim == null)
        {
            TempData["Error"] = "Phiên đăng nhập không hợp lệ. Vui lòng đăng xuất và đăng nhập lại.";
            return RedirectToAction("Index", "Home");
        }
        var tenantId = int.Parse(tenantIdClaim);

        var requests = await context.RentalRequests
            .Include(r => r.Office)
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Tenant)]
    public async Task<IActionResult> Create(int officeId, DateTime startDate, DateTime endDate, string? notes)
    {
        var tenantIdClaim = User.FindFirstValue("TenantId");
        if (tenantIdClaim == null)
        {
            TempData["Error"] = "Phiên đăng nhập không hợp lệ. Vui lòng đăng xuất và đăng nhập lại.";
            return RedirectToAction("Index", "Home");
        }
        var tenantId = int.Parse(tenantIdClaim);

        // Check tenant still exists in DB
        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            TempData["Error"] = "Tài khoản khách thuê không tồn tại. Vui lòng đăng xuất và đăng nhập lại.";
            return RedirectToAction("Logout", "Account");
        }

        var office = await context.Offices.FindAsync(officeId);
        if (office is null)
            return NotFound();

        if (office.Status != OfficeStatuses.Available)
        {
            TempData["Error"] = "Văn phòng này hiện không còn trống.";
            return RedirectToAction("Details", "Offices", new { id = officeId });
        }

        // Validate: start date must not be in the past
        if (startDate.Date < DateTime.Today)
        {
            TempData["Error"] = "Ngày bắt đầu thuê không được là ngày trong quá khứ.";
            return RedirectToAction("Details", "Offices", new { id = officeId });
        }

        if (endDate <= startDate)
        {
            TempData["Error"] = "Ngày kết thúc phải sau ngày bắt đầu.";
            return RedirectToAction("Details", "Offices", new { id = officeId });
        }

        // Check if tenant already has a pending request for this office
        var hasPending = await context.RentalRequests.AnyAsync(r =>
            r.OfficeId == officeId &&
            r.TenantId == tenantId &&
            r.Status == RentalRequestStatuses.Pending);

        if (hasPending)
        {
            TempData["Error"] = "Bạn đã gửi yêu cầu thuê văn phòng này trước đó. Vui lòng chờ phê duyệt.";
            return RedirectToAction("Details", "Offices", new { id = officeId });
        }

        // Check if tenant already has an active contract for this office
        var hasActiveContract = await context.Contracts.AnyAsync(c =>
            c.OfficeId == officeId &&
            c.TenantId == tenantId &&
            c.Status == ContractStatuses.Active);

        if (hasActiveContract)
        {
            TempData["Error"] = "Bạn đã có hợp đồng đang hoạt động cho văn phòng này.";
            return RedirectToAction("Details", "Offices", new { id = officeId });
        }

        var request = new RentalRequest
        {
            OfficeId = officeId,
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            Notes = notes,
            Status = RentalRequestStatuses.Pending
        };

        context.RentalRequests.Add(request);
        await context.SaveChangesAsync();

        TempData["Success"] = "Đã gửi yêu cầu thuê văn phòng. Vui lòng chờ phê duyệt từ Ban quản lý.";
        return RedirectToAction(nameof(MyRequests));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
    public async Task<IActionResult> Reject(int id)
    {
        var request = await context.RentalRequests.FindAsync(id);
        if (request is null) return NotFound();

        if (request.Status != RentalRequestStatuses.Pending)
        {
            TempData["Error"] = "Yêu cầu này đã được xử lý.";
            return RedirectToAction(nameof(Index));
        }

        request.Status = RentalRequestStatuses.Rejected;
        await context.SaveChangesAsync();

        TempData["Success"] = "Đã từ chối yêu cầu thuê.";
        return RedirectToAction(nameof(Index));
    }
}
