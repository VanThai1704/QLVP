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
public class EmployeesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var employees = await context.Employees
            .Include(e => e.Account)
            .OrderBy(e => e.FullName)
            .ToListAsync();
        return View(employees);
    }

    public IActionResult Create() => View(new EmployeeCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await context.Accounts.AnyAsync(a => a.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
            return View(model);
        }

        var role = model.Position switch
        {
            "Manager" => AppRoles.Manager,
            "Accountant" => AppRoles.Accountant,
            "Technician" => AppRoles.Technician,
            _ => AppRoles.Technician
        };

        if (User.IsInRole(AppRoles.Manager) && role == AppRoles.Manager)
        {
            ModelState.AddModelError(nameof(model.Position), "Chỉ quản trị viên mới tạo tài khoản quản lý.");
            return View(model);
        }

        var account = new Account
        {
            Username = model.Username,
            PasswordHash = PasswordHelper.Hash(model.Password),
            Role = role,
            Status = "Active"
        };

        context.Employees.Add(new Employee
        {
            Account = account,
            FullName = model.FullName,
            Phone = model.Phone,
            Email = model.Email,
            Position = model.Position
        });

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã tạo tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employee = await context.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e => e.Id == id);
        return employee is null ? NotFound() : View(employee);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee model)
    {
        if (id != model.Id) return NotFound();

        var employee = await context.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        employee.FullName = model.FullName;
        employee.Phone = model.Phone;
        employee.Email = model.Email;
        employee.Position = model.Position;

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật nhân viên.";
        return RedirectToAction(nameof(Index));
    }
}
