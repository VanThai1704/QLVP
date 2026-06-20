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

        // Wipe existing tables in correct order to avoid FK errors
        context.BankPaymentPending.RemoveRange(context.BankPaymentPending);
        context.InvoiceDetails.RemoveRange(context.InvoiceDetails);
        context.Invoices.RemoveRange(context.Invoices);
        context.MaintenanceRequests.RemoveRange(context.MaintenanceRequests);
        context.RentalRequests.RemoveRange(context.RentalRequests);
        context.Contracts.RemoveRange(context.Contracts);
        context.OfficeServices.RemoveRange(context.OfficeServices);
        context.Offices.RemoveRange(context.Offices);
        context.ServiceTypes.RemoveRange(context.ServiceTypes);
        context.Tenants.RemoveRange(context.Tenants);
        context.Employees.RemoveRange(context.Employees);
        context.Accounts.RemoveRange(context.Accounts);
        await context.SaveChangesAsync();

        // 1. Seed Accounts
        var accounts = new[]
        {
            new Account { Username = "admin", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Admin, Status = "Active" },
            new Account { Username = "quanly01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Manager, Status = "Active" },
            new Account { Username = "ketoan01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Accountant, Status = "Active" },
            new Account { Username = "kythuat01", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Technician, Status = "Active" },
            new Account { Username = "techvina", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Tenant, Status = "Active" },
            new Account { Username = "minhphat", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Tenant, Status = "Active" },
            new Account { Username = "hanoisoft", PasswordHash = PasswordHelper.Hash("123456"), Role = AppRoles.Tenant, Status = "Active" }
        };
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();

        // 2. Seed Employees
        var managerEmp = new Employee { AccountId = accounts[1].Id, FullName = "Nguyễn Văn Quản", Phone = "0901234567", Email = "quan.nguyen@officepro.vn", Position = "Manager" };
        var accountantEmp = new Employee { AccountId = accounts[2].Id, FullName = "Lê Thị Kế", Phone = "0902345678", Email = "ke.le@officepro.vn", Position = "Accountant" };
        var technicianEmp = new Employee { AccountId = accounts[3].Id, FullName = "Trần Văn Kỹ", Phone = "0903456789", Email = "ky.tran@officepro.vn", Position = "Technician" };
        
        context.Employees.AddRange(managerEmp, accountantEmp, technicianEmp);

        // 3. Seed Tenants
        var tenant1 = new Tenant
        {
            AccountId = accounts[4].Id,
            CompanyName = "Công ty TNHH Giải pháp Công nghệ TechVina",
            RepresentativeName = "Nguyễn Văn Công",
            Phone = "0911222333",
            Email = "contact@techvina.vn",
            Address = "Tòa nhà TechVina, Hà Nội"
        };
        var tenant2 = new Tenant
        {
            AccountId = accounts[5].Id,
            CompanyName = "Công ty Cổ phần Thương mại Minh Phát",
            RepresentativeName = "Trần Minh Phát",
            Phone = "0922333444",
            Email = "info@minhphat.com.vn",
            Address = "Quận 1, TP. Hồ Chí Minh"
        };
        var tenant3 = new Tenant
        {
            AccountId = accounts[6].Id,
            CompanyName = "Công ty Cổ phần Phần mềm Hà Nội (HanoiSoft)",
            RepresentativeName = "Phạm Hà Đông",
            Phone = "0933444555",
            Email = "hr@hanoisoft.com",
            Address = "Quận Cầu Giấy, Hà Nội"
        };
        context.Tenants.AddRange(tenant1, tenant2, tenant3);
        await context.SaveChangesAsync();

        // 4. Seed Offices
        var offices = new[]
        {
            new Office { OfficeCode = "OF-101", RoomNumber = "101", Name = "Văn phòng Start-up A", AreaSqm = 45, Capacity = 8, Location = "Tầng 1", MonthlyRent = 8_000_000, Status = OfficeStatuses.Rented, Description = "View hướng đường lộ chính" },
            new Office { OfficeCode = "OF-201", RoomNumber = "201", Name = "Văn phòng Tiêu chuẩn B", AreaSqm = 75, Capacity = 15, Location = "Tầng 2", MonthlyRent = 12_000_000, Status = OfficeStatuses.Available, Description = "Có máy lạnh và bàn ghế cơ bản" },
            new Office { OfficeCode = "OF-202", RoomNumber = "202", Name = "Văn phòng Đại diện C", AreaSqm = 60, Capacity = 12, Location = "Tầng 2", MonthlyRent = 10_000_000, Status = OfficeStatuses.Available, Description = "Thiết kế hiện đại, nhiều ánh sáng" },
            new Office { OfficeCode = "OF-301", RoomNumber = "301", Name = "Văn phòng VIP Penthouse", AreaSqm = 150, Capacity = 30, Location = "Tầng 3", MonthlyRent = 25_000_000, Status = OfficeStatuses.Rented, Description = "Văn phòng cao cấp kèm phòng họp riêng" },
            new Office { OfficeCode = "OF-302", RoomNumber = "302", Name = "Văn phòng Hội nghị", AreaSqm = 100, Capacity = 20, Location = "Tầng 3", MonthlyRent = 18_000_000, Status = OfficeStatuses.Maintenance, Description = "Đang nâng cấp hệ thống cách âm" }
        };
        context.Offices.AddRange(offices);
        await context.SaveChangesAsync();

        // 5. Seed Service Types
        var serviceTypes = new[]
        {
            new ServiceType { Name = "Electricity", Unit = "kWh", DefaultUnitPrice = 3500, IsMetered = true },
            new ServiceType { Name = "Water", Unit = "m3", DefaultUnitPrice = 12000, IsMetered = true },
            new ServiceType { Name = "Internet", Unit = "Tháng", DefaultUnitPrice = 600_000, IsMetered = false },
            new ServiceType { Name = "Cleaning", Unit = "Tháng", DefaultUnitPrice = 400_000, IsMetered = false },
            new ServiceType { Name = "Parking", Unit = "Tháng", DefaultUnitPrice = 1_200_000, IsMetered = false }
        };
        context.ServiceTypes.AddRange(serviceTypes);
        await context.SaveChangesAsync();

        // 6. Seed Office Services
        var officeServices = new[]
        {
            // OF-101
            new OfficeService { OfficeId = offices[0].Id, ServiceTypeId = serviceTypes[0].Id, UnitPrice = 3500 },
            new OfficeService { OfficeId = offices[0].Id, ServiceTypeId = serviceTypes[1].Id, UnitPrice = 12000 },
            new OfficeService { OfficeId = offices[0].Id, ServiceTypeId = serviceTypes[2].Id, UnitPrice = 600_000 },
            new OfficeService { OfficeId = offices[0].Id, ServiceTypeId = serviceTypes[3].Id, UnitPrice = 400_000 },
            // OF-301
            new OfficeService { OfficeId = offices[3].Id, ServiceTypeId = serviceTypes[0].Id, UnitPrice = 3500 },
            new OfficeService { OfficeId = offices[3].Id, ServiceTypeId = serviceTypes[1].Id, UnitPrice = 12000 },
            new OfficeService { OfficeId = offices[3].Id, ServiceTypeId = serviceTypes[2].Id, UnitPrice = 600_000 },
            new OfficeService { OfficeId = offices[3].Id, ServiceTypeId = serviceTypes[3].Id, UnitPrice = 400_000 },
            new OfficeService { OfficeId = offices[3].Id, ServiceTypeId = serviceTypes[4].Id, UnitPrice = 1_200_000 }
        };
        context.OfficeServices.AddRange(officeServices);
        await context.SaveChangesAsync();

        // 7. Seed Contracts
        var contracts = new[]
        {
            new Contract
            {
                ContractCode = "HD-TECH01",
                SignedDate = DateTime.Today.AddMonths(-5),
                StartDate = DateTime.Today.AddMonths(-5),
                EndDate = DateTime.Today.AddMonths(7),
                DepositAmount = 16_000_000,
                MonthlyRent = 8_000_000,
                Terms = "Thanh toán vào ngày 5 hàng tháng. Hợp đồng có hiệu lực 1 năm.",
                Status = ContractStatuses.Active,
                TenantId = tenant1.Id,
                OfficeId = offices[0].Id,
                CreatedByEmployeeId = managerEmp.Id
            },
            new Contract
            {
                ContractCode = "HD-MINH01",
                SignedDate = DateTime.Today.AddMonths(-4),
                StartDate = DateTime.Today.AddMonths(-4),
                EndDate = DateTime.Today.AddMonths(8),
                DepositAmount = 50_000_000,
                MonthlyRent = 25_000_000,
                Terms = "Đặt cọc 2 tháng tiền thuê. Dọn dẹp vệ sinh 3 lần/tuần.",
                Status = ContractStatuses.Active,
                TenantId = tenant2.Id,
                OfficeId = offices[3].Id,
                CreatedByEmployeeId = managerEmp.Id
            },
            new Contract
            {
                ContractCode = "HD-HANOI01",
                SignedDate = DateTime.Today.AddYears(-1).AddMonths(-2),
                StartDate = DateTime.Today.AddYears(-1).AddMonths(-2),
                EndDate = DateTime.Today.AddMonths(-2),
                DepositAmount = 20_000_000,
                MonthlyRent = 10_000_000,
                Terms = "Hợp đồng đã kết thúc thời hạn cho thuê.",
                Status = ContractStatuses.Expired,
                TenantId = tenant3.Id,
                OfficeId = offices[2].Id,
                CreatedByEmployeeId = managerEmp.Id
            }
        };
        context.Contracts.AddRange(contracts);
        await context.SaveChangesAsync();

        // 8. Seed Rental Requests
        var rentalRequests = new[]
        {
            new RentalRequest
            {
                OfficeId = offices[1].Id,
                TenantId = tenant3.Id,
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(5).AddYears(1),
                Status = RentalRequestStatuses.Pending,
                Notes = "HanoiSoft đăng ký thuê thêm văn phòng 201 để mở rộng dự án mới.",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new RentalRequest
            {
                OfficeId = offices[0].Id,
                TenantId = tenant1.Id,
                StartDate = DateTime.Today.AddMonths(-5),
                EndDate = DateTime.Today.AddMonths(-5).AddYears(1),
                Status = RentalRequestStatuses.Approved,
                Notes = "Yêu cầu thuê phòng 101.",
                CreatedAt = DateTime.UtcNow.AddMonths(-5).AddDays(-5)
            },
            new RentalRequest
            {
                OfficeId = offices[4].Id,
                TenantId = tenant2.Id,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(1).AddYears(1),
                Status = RentalRequestStatuses.Rejected,
                Notes = "Yêu cầu đặt thuê phòng hội nghị tầng 3.",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };
        context.RentalRequests.AddRange(rentalRequests);
        await context.SaveChangesAsync();

        // 9. Seed Maintenance Requests
        var maintenanceRequests = new[]
        {
            new MaintenanceRequest
            {
                RequestCode = "MR-001",
                OfficeId = offices[0].Id,
                TenantId = tenant1.Id,
                AssignedEmployeeId = technicianEmp.Id,
                Description = "Hệ thống điều hòa phòng 101 bị rò rỉ nước và làm mát kém.",
                Priority = MaintenancePriorities.High,
                Status = MaintenanceStatuses.InProgress,
                CreatedDate = DateTime.Today.AddDays(-3)
            },
            new MaintenanceRequest
            {
                RequestCode = "MR-002",
                OfficeId = offices[3].Id,
                TenantId = tenant2.Id,
                AssignedEmployeeId = technicianEmp.Id,
                Description = "Thay thế 3 bóng đèn LED bị cháy ở phòng họp chính.",
                Priority = MaintenancePriorities.Normal,
                Status = MaintenanceStatuses.Completed,
                CreatedDate = DateTime.Today.AddDays(-7),
                CompletedDate = DateTime.Today.AddDays(-6)
            },
            new MaintenanceRequest
            {
                RequestCode = "MR-003",
                OfficeId = offices[0].Id,
                TenantId = tenant1.Id,
                AssignedEmployeeId = null,
                Description = "Bản lề cửa ra vào bị kẹt rỉ sét, phát ra tiếng kêu cọt kẹt.",
                Priority = MaintenancePriorities.Low,
                Status = MaintenanceStatuses.Pending,
                CreatedDate = DateTime.Today.AddDays(-1)
            }
        };
        context.MaintenanceRequests.AddRange(maintenanceRequests);
        await context.SaveChangesAsync();

        // 10. Seed Invoices & InvoiceDetails
        var invoice1 = new Invoice
        {
            InvoiceCode = "INV-TE05",
            ContractId = contracts[0].Id,
            BillingMonth = (byte)DateTime.Today.AddMonths(-1).Month,
            BillingYear = (short)DateTime.Today.AddMonths(-1).Year,
            IssueDate = DateTime.Today.AddMonths(-1).AddDays(5),
            RentAmount = 8_000_000,
            ServicesSubtotal = 1_525_000,
            TotalAmount = 9_525_000,
            Status = InvoiceStatuses.Paid,
            PaidDate = DateTime.Today.AddMonths(-1).AddDays(7),
            PaymentMethod = PaymentMethods.BankTransfer
        };
        context.Invoices.Add(invoice1);
        await context.SaveChangesAsync();

        var invoiceDetails1 = new[]
        {
            new InvoiceDetail { InvoiceId = invoice1.Id, OfficeServiceId = officeServices[0].Id, PreviousReading = 1000, CurrentReading = 1150, UnitPrice = 3500 }, // Điện: 150 kWh
            new InvoiceDetail { InvoiceId = invoice1.Id, OfficeServiceId = officeServices[1].Id, PreviousReading = 200, CurrentReading = 210, UnitPrice = 12000 },  // Nước: 10 m3
            new InvoiceDetail { InvoiceId = invoice1.Id, OfficeServiceId = officeServices[2].Id, PreviousReading = 0, CurrentReading = 1, UnitPrice = 600000 },   // Internet
            new InvoiceDetail { InvoiceId = invoice1.Id, OfficeServiceId = officeServices[3].Id, PreviousReading = 0, CurrentReading = 1, UnitPrice = 400000 }    // Cleaning
        };
        context.InvoiceDetails.AddRange(invoiceDetails1);

        var invoice2 = new Invoice
        {
            InvoiceCode = "INV-TE06",
            ContractId = contracts[0].Id,
            BillingMonth = (byte)DateTime.Today.Month,
            BillingYear = (short)DateTime.Today.Year,
            IssueDate = DateTime.Today.AddDays(-5),
            RentAmount = 8_000_000,
            ServicesSubtotal = 1_000_000, // Internet + Vệ sinh cố định
            TotalAmount = 9_000_000,
            Status = InvoiceStatuses.Unpaid,
            PaidDate = null,
            PaymentMethod = null
        };
        context.Invoices.Add(invoice2);
        await context.SaveChangesAsync();

        var invoiceDetails2 = new[]
        {
            new InvoiceDetail { InvoiceId = invoice2.Id, OfficeServiceId = officeServices[2].Id, PreviousReading = 0, CurrentReading = 1, UnitPrice = 600000 },
            new InvoiceDetail { InvoiceId = invoice2.Id, OfficeServiceId = officeServices[3].Id, PreviousReading = 0, CurrentReading = 1, UnitPrice = 400000 }
        };
        context.InvoiceDetails.AddRange(invoiceDetails2);

        var invoice3 = new Invoice
        {
            InvoiceCode = "INV-HN04",
            ContractId = contracts[2].Id,
            BillingMonth = (byte)DateTime.Today.AddMonths(-2).Month,
            BillingYear = (short)DateTime.Today.AddMonths(-2).Year,
            IssueDate = DateTime.Today.AddMonths(-2).AddDays(5),
            RentAmount = 10_000_000,
            ServicesSubtotal = 0,
            TotalAmount = 10_000_000,
            Status = InvoiceStatuses.Overdue,
            PaidDate = null,
            PaymentMethod = null
        };
        context.Invoices.Add(invoice3);
        await context.SaveChangesAsync();
    }
}
