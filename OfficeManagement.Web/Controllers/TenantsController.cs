using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;

using OfficeManagement.Web.Models.ViewModels;
using OfficeManagement.Web.Services;

namespace OfficeManagement.Web.Controllers;

[AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
public class TenantsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var tenants = await context.Tenants
            .Include(t => t.Account)
            .Include(t => t.Contracts)
            .OrderBy(t => t.CompanyName)
            .ToListAsync();
        return View(tenants);
    }

    public IActionResult Create() => View(new TenantCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await context.Accounts.AnyAsync(a => a.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
            return View(model);
        }

        if (await context.Tenants.AnyAsync(t => t.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
            return View(model);
        }

        context.Tenants.Add(new Tenant
        {
            Account = new Account
            {
                Username = model.Username,
                PasswordHash = PasswordHelper.Hash(model.Password),
                Role = AppRoles.Tenant,
                Status = "Active"
            },
            CompanyName = model.CompanyName,
            RepresentativeName = model.RepresentativeName,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address
        });

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã thêm khách thuê mới.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tenant = await context.Tenants.FindAsync(id);
        return tenant is null ? NotFound() : View(tenant);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Tenant tenant)
    {
        if (id != tenant.Id) return NotFound();
        if (!ModelState.IsValid) return View(tenant);

        context.Update(tenant);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thông tin khách thuê.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var tenant = await context.Tenants
            .Include(t => t.Contracts).ThenInclude(c => c.Office)
            .Include(t => t.MaintenanceRequests)
            .FirstOrDefaultAsync(t => t.Id == id);
        return tenant is null ? NotFound() : View(tenant);
    }
}
