using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;

namespace OfficeManagement.Web.Controllers;

[AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
public class ServiceTypesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await context.ServiceTypes.OrderBy(s => s.Name).ToListAsync());

    public IActionResult Create() => View(new ServiceType());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceType model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await context.ServiceTypes.AnyAsync(s => s.Name == model.Name))
        {
            ModelState.AddModelError(nameof(ServiceType.Name), "Tên dịch vụ đã tồn tại.");
            return View(model);
        }

        context.ServiceTypes.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã thêm loại dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await context.ServiceTypes.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceType model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật loại dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.ServiceTypes.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await context.ServiceTypes
            .Include(s => s.OfficeServices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (item is null) return NotFound();
        if (item.OfficeServices.Any())
        {
            TempData["Error"] = "Không thể xóa dịch vụ đang được gán cho văn phòng.";
            return RedirectToAction(nameof(Index));
        }

        context.ServiceTypes.Remove(item);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa loại dịch vụ.";
        return RedirectToAction(nameof(Index));
    }
}

[AuthorizeRole(AppRoles.Manager, AppRoles.Admin)]
public class OfficeServicesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await context.OfficeServices
            .Include(o => o.Office)
            .Include(o => o.ServiceType)
            .OrderBy(o => o.Office!.OfficeCode)
            .ThenBy(o => o.ServiceType!.Name)
            .ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Offices = await context.Offices.OrderBy(o => o.OfficeCode).ToListAsync();
        ViewBag.ServiceTypes = await context.ServiceTypes.OrderBy(s => s.Name).ToListAsync();
        return View(new OfficeService());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OfficeService model)
    {
        ViewBag.Offices = await context.Offices.OrderBy(o => o.OfficeCode).ToListAsync();
        ViewBag.ServiceTypes = await context.ServiceTypes.OrderBy(s => s.Name).ToListAsync();

        if (!ModelState.IsValid) return View(model);

        var serviceType = await context.ServiceTypes.FindAsync(model.ServiceTypeId);
        if (serviceType is not null && model.UnitPrice == 0)
            model.UnitPrice = serviceType.DefaultUnitPrice;

        if (await context.OfficeServices.AnyAsync(o => o.OfficeId == model.OfficeId && o.ServiceTypeId == model.ServiceTypeId))
        {
            ModelState.AddModelError(string.Empty, "Dịch vụ này đã được gán cho văn phòng.");
            return View(model);
        }

        context.OfficeServices.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã gán dịch vụ cho văn phòng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await context.OfficeServices
            .Include(o => o.Office)
            .Include(o => o.ServiceType)
            .FirstOrDefaultAsync(o => o.Id == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OfficeService model)
    {
        if (id != model.Id) return NotFound();

        var item = await context.OfficeServices.FindAsync(id);
        if (item is null) return NotFound();

        item.UnitPrice = model.UnitPrice;
        item.IsActive = model.IsActive;
        await context.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật dịch vụ văn phòng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.OfficeServices
            .Include(o => o.Office)
            .Include(o => o.ServiceType)
            .FirstOrDefaultAsync(o => o.Id == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await context.OfficeServices.FindAsync(id);
        if (item is null) return NotFound();

        context.OfficeServices.Remove(item);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã gỡ dịch vụ khỏi văn phòng.";
        return RedirectToAction(nameof(Index));
    }
}
