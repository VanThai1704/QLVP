using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;
using OfficeManagement.Web.Models.ViewModels;
using OfficeManagement.Web.Services;

namespace OfficeManagement.Web.Controllers;

public class AccountController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var account = await context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.Username == model.Username && a.Status == "Active");

        if (account is null || !PasswordHelper.Verify(model.Password, account.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        await AuthHelper.SignInAsync(HttpContext, account);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
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

        var account = new Account
        {
            Username = model.Username,
            PasswordHash = PasswordHelper.Hash(model.Password),
            Role = AppRoles.Tenant,
            Status = "Active"
        };

        var tenant = new Tenant
        {
            Account = account,
            CompanyName = model.CompanyName,
            RepresentativeName = model.RepresentativeName,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        await context.Entry(account).Reference(a => a.Tenant).LoadAsync();
        await AuthHelper.SignInAsync(HttpContext, account);

        TempData["Success"] = "Đăng ký thành công. Chào mừng bạn!";
        return RedirectToAction("Index", "Home");
    }

    [AuthorizeRole]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var accountId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var account = await context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Tenant)
            .FirstAsync(a => a.Id == accountId);

        var vm = new ProfileViewModel
        {
            Username = account.Username,
            Role = account.Role,
            Phone = account.Employee?.Phone ?? account.Tenant?.Phone,
            Email = account.Employee?.Email ?? account.Tenant?.Email,
            Address = account.Tenant?.Address,
            DisplayName = account.Employee?.FullName ?? account.Tenant?.CompanyName ?? account.Username
        };

        return View(vm);
    }

    [AuthorizeRole]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var accountId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var account = await context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Tenant)
            .FirstAsync(a => a.Id == accountId);

        if (account.Employee is not null)
        {
            account.Employee.FullName = model.DisplayName;
            account.Employee.Phone = model.Phone;
            account.Employee.Email = model.Email;
        }
        else if (account.Tenant is not null)
        {
            account.Tenant.CompanyName = model.DisplayName;
            account.Tenant.Phone = model.Phone;
            account.Tenant.Email = model.Email;
            account.Tenant.Address = model.Address;
        }

        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Profile));
    }

    [AuthorizeRole]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [AuthorizeRole]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var accountId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var account = await context.Accounts.FindAsync(accountId);

        if (account is null || !PasswordHelper.Verify(model.CurrentPassword, account.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không đúng.");
            return View(model);
        }

        account.PasswordHash = PasswordHelper.Hash(model.NewPassword);
        await context.SaveChangesAsync();

        TempData["Success"] = "Đã đổi mật khẩu thành công.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();
}
