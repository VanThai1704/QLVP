using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeManagement.Web.Models.Constants;

namespace OfficeManagement.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public AuthorizeRoleAttribute(params string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        if (_roles.Length == 0)
            return;

        var role = user.FindFirstValue(ClaimTypes.Role);
        if (role is null || (!_roles.Contains(role) && role != AppRoles.Admin))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
