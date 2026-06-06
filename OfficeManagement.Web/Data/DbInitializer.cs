using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;
using OfficeManagement.Web.Services;

namespace OfficeManagement.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();
        await RefreshDemoDataAsync(context);

        if (await context.Accounts.AnyAsync())
            return;

        var accounts = new[]
        {
            new Account { Username = "vy01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Tenant, Status = "Active" },
            new Account { Username = "tuan01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Tenant, Status = "Active" },
            new Account { Username = "toan01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Tenant, Status = "Active" },
            new Account { Username = "huy01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Manager, Status = "Active" },
            new Account { Username = "ktoan01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Accountant, Status = "Active" },
            new Account { Username = "thai01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Technician, Status = "Active" },
            new Account { Username = "admin", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Admin, Status = "Active" },
        };
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();

        context.Employees.AddRange(
            new Employee { AccountId = accounts[3].Id, FullName = "Le Quang Huy", Phone = "0911111111", Email = "huy@gmail.com", Position = "Manager" },
            new Employee { AccountId = accounts[4].Id, FullName = "Tran Thi Ke Toan", Phone = "0911222333", Email = "ktoan@gmail.com", Position = "Accountant" },
            new Employee { AccountId = accounts[5].Id, FullName = "Anh Thai", Phone = "0922222222", Email = "thai@gmail.com", Position = "Technician" }
        );

        context.Tenants.AddRange(
            new Tenant { AccountId = accounts[0].Id, CompanyName = "Vy Tech Co., Ltd.", RepresentativeName = "Mai Ha Thanh Vy", Phone = "0933333333", Email = "vy@gmail.com", Address = "Can Tho" },
            new Tenant { AccountId = accounts[1].Id, CompanyName = "Tuan Software Co., Ltd.", RepresentativeName = "Nguyen Minh Tuan", Phone = "0944444444", Email = "tuan@gmail.com", Address = "Dong Thap" },
            new Tenant { AccountId = accounts[2].Id, CompanyName = "Toan Security Co., Ltd.", RepresentativeName = "Ngo Le Thanh Toan", Phone = "0955555555", Email = "toan@gmail.com", Address = "An Giang" }
        );
        await context.SaveChangesAsync();

        context.Offices.AddRange(
            new Office { OfficeCode = "OF-001", RoomNumber = "101", Name = "Office A", AreaSqm = 50, Capacity = 8, Location = "Floor 1", MonthlyRent = 10_000_000, Status = OfficeStatuses.Available, Description = "Street-facing view" },
            new Office { OfficeCode = "OF-002", RoomNumber = "201", Name = "Office B", AreaSqm = 80, Capacity = 15, Location = "Floor 2", MonthlyRent = 15_000_000, Status = OfficeStatuses.Available, Description = "Air-conditioned" },
            new Office { OfficeCode = "OF-003", RoomNumber = "301", Name = "Office C", AreaSqm = 120, Capacity = 25, Location = "Floor 3", MonthlyRent = 20_000_000, Status = OfficeStatuses.Available, Description = "VIP room" }
        );

        context.ServiceTypes.AddRange(
            new ServiceType { Name = "Electricity", Unit = "kWh", DefaultUnitPrice = 3500, IsMetered = true },
            new ServiceType { Name = "Water", Unit = "m3", DefaultUnitPrice = 12000, IsMetered = true },
            new ServiceType { Name = "Internet", Unit = "Month", DefaultUnitPrice = 500_000, IsMetered = false },
            new ServiceType { Name = "Cleaning", Unit = "Month", DefaultUnitPrice = 300_000, IsMetered = false },
            new ServiceType { Name = "Security", Unit = "Month", DefaultUnitPrice = 200_000, IsMetered = false }
        );
        await context.SaveChangesAsync();

        var manager = await context.Employees.FirstAsync(e => e.Position == "Manager");
        var technician = await context.Employees.FirstAsync(e => e.Position == "Technician");
        var tenants = await context.Tenants.OrderBy(t => t.Id).ToListAsync();
        var offices = await context.Offices.OrderBy(o => o.Id).ToListAsync();
        var serviceTypes = await context.ServiceTypes.ToListAsync();

        context.OfficeServices.AddRange(
            new OfficeService { OfficeId = offices[0].Id, ServiceTypeId = serviceTypes[0].Id, UnitPrice = 3500 },
            new OfficeService { OfficeId = offices[0].Id, ServiceTypeId = serviceTypes[1].Id, UnitPrice = 12000 },
            new OfficeService { OfficeId = offices[1].Id, ServiceTypeId = serviceTypes[2].Id, UnitPrice = 500_000 },
            new OfficeService { OfficeId = offices[2].Id, ServiceTypeId = serviceTypes[0].Id, UnitPrice = 3500 }
        );

        var today = DateTime.Today;
        var contractStart = new DateTime(today.Year, 1, 1);
        var contractEnd1 = today.AddYears(1);
        var contractEnd2 = today.AddYears(1).AddMonths(1);
        var contractEnd3 = today.AddYears(1).AddMonths(2);

        context.Contracts.AddRange(
            new Contract { ContractCode = "CT-001", SignedDate = contractStart, StartDate = contractStart, EndDate = contractEnd1, DepositAmount = 5_000_000, MonthlyRent = 10_000_000, Terms = "Rent due before the 5th of each month.", Status = ContractStatuses.Active, TenantId = tenants[0].Id, OfficeId = offices[0].Id, CreatedByEmployeeId = manager.Id },
            new Contract { ContractCode = "CT-002", SignedDate = contractStart.AddMonths(1), StartDate = contractStart.AddMonths(1), EndDate = contractEnd2, DepositAmount = 7_000_000, MonthlyRent = 15_000_000, Terms = "Tenant must not modify office structure without approval.", Status = ContractStatuses.Active, TenantId = tenants[1].Id, OfficeId = offices[1].Id, CreatedByEmployeeId = manager.Id },
            new Contract { ContractCode = "CT-003", SignedDate = contractStart.AddMonths(2), StartDate = contractStart.AddMonths(2), EndDate = contractEnd3, DepositAmount = 8_000_000, MonthlyRent = 20_000_000, Terms = "Tenant is responsible for office asset care.", Status = ContractStatuses.Active, TenantId = tenants[2].Id, OfficeId = offices[2].Id, CreatedByEmployeeId = manager.Id }
        );

        foreach (var office in offices)
            office.Status = OfficeStatuses.Rented;

        await context.SaveChangesAsync();

        var contracts = await context.Contracts.OrderBy(c => c.Id).ToListAsync();
        var officeServices = await context.OfficeServices.OrderBy(o => o.Id).ToListAsync();

        var billingMonth = (byte)today.Month;
        var billingYear = (short)today.Year;
        var issueDate = today.AddDays(-7);

        var invoice1 = new Invoice { InvoiceCode = "INV-001", ContractId = contracts[0].Id, BillingMonth = billingMonth, BillingYear = billingYear, IssueDate = issueDate, RentAmount = 10_000_000, Status = InvoiceStatuses.Unpaid };
        invoice1.Details.Add(new InvoiceDetail { OfficeServiceId = officeServices[0].Id, PreviousReading = 100, CurrentReading = 150, UnitPrice = 3500 });
        invoice1.Details.Add(new InvoiceDetail { OfficeServiceId = officeServices[1].Id, PreviousReading = 10, CurrentReading = 15, UnitPrice = 12000 });
        invoice1.RecalculateTotals();

        var invoice2 = new Invoice { InvoiceCode = "INV-002", ContractId = contracts[1].Id, BillingMonth = billingMonth, BillingYear = billingYear, IssueDate = issueDate, RentAmount = 15_000_000, Status = InvoiceStatuses.Unpaid };
        invoice2.Details.Add(new InvoiceDetail { OfficeServiceId = officeServices[2].Id, PreviousReading = 0, CurrentReading = 1, UnitPrice = 500_000 });
        invoice2.RecalculateTotals();

        var invoice3 = new Invoice { InvoiceCode = "INV-003", ContractId = contracts[2].Id, BillingMonth = billingMonth, BillingYear = billingYear, IssueDate = issueDate, RentAmount = 20_000_000, Status = InvoiceStatuses.Unpaid };
        invoice3.Details.Add(new InvoiceDetail { OfficeServiceId = officeServices[3].Id, PreviousReading = 200, CurrentReading = 260, UnitPrice = 3500 });
        invoice3.RecalculateTotals();

        context.Invoices.AddRange(invoice1, invoice2, invoice3);

        context.MaintenanceRequests.AddRange(
            new MaintenanceRequest { RequestCode = "MR-001", OfficeId = offices[0].Id, TenantId = tenants[0].Id, AssignedEmployeeId = technician.Id, Description = "Air conditioner not working", Priority = MaintenancePriorities.High, Status = MaintenanceStatuses.InProgress, CreatedDate = today.AddDays(-20) },
            new MaintenanceRequest { RequestCode = "MR-002", OfficeId = offices[1].Id, TenantId = tenants[1].Id, AssignedEmployeeId = technician.Id, Description = "Broken ceiling light", Priority = MaintenancePriorities.Normal, Status = MaintenanceStatuses.Completed, CreatedDate = today.AddDays(-15), CompletedDate = today.AddDays(-12) },
            new MaintenanceRequest { RequestCode = "MR-003", OfficeId = offices[2].Id, TenantId = tenants[2].Id, Description = "Water leak in restroom", Priority = MaintenancePriorities.Urgent, Status = MaintenanceStatuses.Pending, CreatedDate = today.AddDays(-10) }
        );

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Keeps seeded demo contracts/invoices usable when their dates have passed.
    /// </summary>
    private static async Task RefreshDemoDataAsync(ApplicationDbContext context)
    {
        var today = DateTime.Today;
        var demoContracts = await context.Contracts
            .Include(c => c.Office)
            .Where(c => c.ContractCode.StartsWith("CT-"))
            .ToListAsync();

        if (demoContracts.Count == 0)
            return;

        var changed = false;
        foreach (var contract in demoContracts)
        {
            if (contract.Status == ContractStatuses.Terminated)
                continue;

            if (contract.Status == ContractStatuses.Expired || contract.EndDate < today)
            {
                contract.Status = ContractStatuses.Active;
                contract.StartDate = new DateTime(today.Year, 1, 1);
                contract.EndDate = today.AddYears(1);
                if (contract.Office is not null)
                    contract.Office.Status = OfficeStatuses.Rented;
                changed = true;
            }
        }

        var demoInvoices = await context.Invoices
            .Where(i => i.InvoiceCode.StartsWith("INV-"))
            .ToListAsync();

        foreach (var invoice in demoInvoices)
        {
            if (invoice.Status == InvoiceStatuses.Paid || invoice.Status == InvoiceStatuses.Cancelled)
                continue;

            if (invoice.Status == InvoiceStatuses.Overdue || invoice.IssueDate.AddDays(30) < today)
            {
                invoice.Status = InvoiceStatuses.Unpaid;
                invoice.IssueDate = today.AddDays(-7);
                invoice.BillingMonth = (byte)today.Month;
                invoice.BillingYear = (short)today.Year;
                invoice.PaidDate = null;
                changed = true;
            }
        }

        if (changed)
            await context.SaveChangesAsync();

        var mr001 = await context.MaintenanceRequests.FirstOrDefaultAsync(m => m.RequestCode == "MR-001");
        var technician = await context.Employees.FirstOrDefaultAsync(e => e.Position == "Technician");
        if (mr001 is not null && technician is not null &&
            mr001.AssignedEmployeeId != technician.Id &&
            mr001.Status != MaintenanceStatuses.Completed)
        {
            mr001.AssignedEmployeeId = technician.Id;
            await context.SaveChangesAsync();
        }
    }
}
