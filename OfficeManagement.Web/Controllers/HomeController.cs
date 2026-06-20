using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Filters;
using OfficeManagement.Web.Models;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.ViewModels;
using OfficeManagement.Web.Services;

namespace OfficeManagement.Web.Controllers;

[AuthorizeRole]
public class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("Login", "Account");

        await BusinessRulesService.RefreshContractStatusesAsync(context);

        var role = User.FindFirstValue(ClaimTypes.Role);
        var today = DateTime.Today;

        if (role == AppRoles.Tenant)
        {
            var tenantId = int.Parse(User.FindFirstValue("TenantId")!);
            ViewBag.MyContracts = await context.Contracts
                .Include(c => c.Office)
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(c => c.StartDate)
                .Take(5)
                .ToListAsync();

            ViewBag.MyInvoices = await context.Invoices
                .Include(i => i.Contract).ThenInclude(c => c!.Office)
                .Where(i => i.Contract!.TenantId == tenantId)
                .OrderByDescending(i => i.IssueDate)
                .Take(5)
                .ToListAsync();

            ViewBag.AvailableOffices = await context.Offices
                .Where(o => o.Status == OfficeStatuses.Available)
                .OrderBy(o => o.RoomNumber)
                .Take(6)
                .ToListAsync();

            return View("TenantDashboard");
        }

        if (role == AppRoles.Technician)
        {
            var employeeId = int.Parse(User.FindFirstValue("EmployeeId")!);
            ViewBag.AssignedRepairs = await context.MaintenanceRequests
                .Include(m => m.Office)
                .Include(m => m.Tenant)
                .Where(m => m.AssignedEmployeeId == employeeId && m.Status != MaintenanceStatuses.Completed)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();
            return View("TechnicianDashboard");
        }

        var vm = new DashboardViewModel
        {
            TotalOffices = await context.Offices.CountAsync(),
            AvailableOffices = await context.Offices.CountAsync(o => o.Status == OfficeStatuses.Available),
            RentedOffices = await context.Offices.CountAsync(o => o.Status == OfficeStatuses.Rented),
            MaintenanceOffices = await context.Offices.CountAsync(o => o.Status == OfficeStatuses.Maintenance),
            ActiveContracts = await context.Contracts.CountAsync(c => c.Status == ContractStatuses.Active),
            PendingRepairs = await context.MaintenanceRequests.CountAsync(m => m.Status == MaintenanceStatuses.Pending),
            UnpaidInvoices = await context.Invoices.CountAsync(i => i.Status == InvoiceStatuses.Unpaid),
            MonthlyRevenue = await context.Invoices
                .Where(i => i.Status == InvoiceStatuses.Paid && i.PaidDate!.Value.Month == today.Month && i.PaidDate.Value.Year == today.Year)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0
        };

        var revenueGroups = await context.Invoices
            .Where(i => i.Status == InvoiceStatuses.Paid && i.PaidDate != null)
            .GroupBy(i => new { i.PaidDate!.Value.Year, i.PaidDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Take(6)
            .ToListAsync();

        vm.RevenueByMonth = revenueGroups
            .Select(x => new RevenueStatItem
            {
                Period = $"{x.Month:00}/{x.Year}",
                Amount = x.Amount
            })
            .ToList();

        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
