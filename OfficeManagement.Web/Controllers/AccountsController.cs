using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;
using OfficeManagement.Web.Models.ViewModels;
using OfficeManagement.Web.Services;

namespace OfficeManagement.Web.Controllers;

[AuthorizeRole(AppRoles.Admin)]
public class AccountsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var accounts = await context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Tenant)
            .OrderBy(a => a.Username)
            .Select(a => new AccountManageViewModel
            {
                Id = a.Id,
                Username = a.Username,
                Role = a.Role,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                LinkedName = a.Employee != null ? a.Employee.FullName : a.Tenant != null ? a.Tenant.CompanyName : null
            })
            .ToListAsync();

        return View(accounts);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account is null) return NotFound();

        var currentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (account.Id == currentId)
        {
            TempData["Error"] = "Không thể khóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        account.Status = account.Status == "Active" ? "Locked" : "Active";
        await context.SaveChangesAsync();

        TempData["Success"] = account.Status == "Active" ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return RedirectToAction(nameof(Index));
        }

        var account = await context.Accounts.FindAsync(id);
        if (account is null) return NotFound();

        account.PasswordHash = PasswordHelper.Hash(newPassword);
        await context.SaveChangesAsync();

        TempData["Success"] = $"Đã đặt lại mật khẩu cho {account.Username}.";
        return RedirectToAction(nameof(Index));
    }
}
