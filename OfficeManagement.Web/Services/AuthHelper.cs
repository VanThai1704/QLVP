using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OfficeManagement.Web.Models.Entities;

namespace OfficeManagement.Web.Services;

public static class AuthHelper
{
    public static async Task SignInAsync(HttpContext httpContext, Account account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Role, account.Role)
        };

        if (account.Employee is not null)
            claims.Add(new Claim("EmployeeId", account.Employee.Id.ToString()));
        if (account.Tenant is not null)
            claims.Add(new Claim("TenantId", account.Tenant.Id.ToString()));

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }
}
