using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Data;
using OfficeManagement.Web.Models.Constants;

namespace OfficeManagement.Web.Services;

public static class BusinessRulesService
{
    public static async Task RefreshContractStatusesAsync(ApplicationDbContext context)
    {
        var today = DateTime.Today;
        var expired = await context.Contracts
            .Include(c => c.Office)
            .Where(c => c.Status == ContractStatuses.Active && c.EndDate < today)
            .ToListAsync();

        foreach (var contract in expired)
        {
            contract.Status = ContractStatuses.Expired;
            if (contract.Office is not null)
            {
                var hasOther = await context.Contracts.AnyAsync(c =>
                    c.OfficeId == contract.OfficeId &&
                    c.Id != contract.Id &&
                    c.Status == ContractStatuses.Active);
                if (!hasOther)
                    contract.Office.Status = OfficeStatuses.Available;
            }
        }

        var overdueInvoices = await context.Invoices
            .Where(i => i.Status == InvoiceStatuses.Unpaid && i.IssueDate.AddDays(30) < today)
            .ToListAsync();

        foreach (var invoice in overdueInvoices)
            invoice.Status = InvoiceStatuses.Overdue;

        if (expired.Count > 0 || overdueInvoices.Count > 0)
            await context.SaveChangesAsync();
    }
}
