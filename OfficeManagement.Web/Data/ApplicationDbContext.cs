using Microsoft.EntityFrameworkCore;
using OfficeManagement.Web.Models.Entities;

namespace OfficeManagement.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<OfficeService> OfficeServices => Set<OfficeService>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<BankPaymentPending> BankPaymentPending => Set<BankPaymentPending>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.AccountId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Account).WithOne(x => x.Employee)
                .HasForeignKey<Employee>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(x => x.AccountId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Account).WithOne(x => x.Tenant)
                .HasForeignKey<Tenant>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Office>(e =>
        {
            e.Property(x => x.AreaSqm).HasPrecision(10, 2);
            e.Property(x => x.MonthlyRent).HasPrecision(18, 2);
            e.HasIndex(x => x.OfficeCode).IsUnique();
            e.HasIndex(x => x.RoomNumber).IsUnique();
        });

        modelBuilder.Entity<Contract>(e =>
        {
            e.Property(x => x.DepositAmount).HasPrecision(18, 2);
            e.Property(x => x.MonthlyRent).HasPrecision(18, 2);
            e.HasIndex(x => x.ContractCode).IsUnique();
            e.HasIndex(x => new { x.OfficeId, x.Status });
            e.HasOne(x => x.Tenant).WithMany(x => x.Contracts).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Office).WithMany(x => x.Contracts).HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByEmployee).WithMany(x => x.CreatedContracts).HasForeignKey(x => x.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceType>(e =>
        {
            e.Property(x => x.DefaultUnitPrice).HasPrecision(18, 2);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<OfficeService>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(x => new { x.OfficeId, x.ServiceTypeId }).IsUnique();
            e.HasOne(x => x.Office).WithMany(x => x.OfficeServices).HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ServiceType).WithMany(x => x.OfficeServices).HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(x => x.RentAmount).HasPrecision(18, 2);
            e.Property(x => x.ServicesSubtotal).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.InvoiceCode).IsUnique();
            e.HasIndex(x => new { x.ContractId, x.BillingMonth, x.BillingYear }).IsUnique();
            e.HasOne(x => x.Contract).WithMany(x => x.Invoices).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceDetail>(e =>
        {
            e.Property(x => x.PreviousReading).HasPrecision(18, 2);
            e.Property(x => x.CurrentReading).HasPrecision(18, 2);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(x => new { x.InvoiceId, x.OfficeServiceId }).IsUnique();
            e.Ignore(x => x.Quantity);
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.Invoice).WithMany(x => x.Details).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OfficeService).WithMany(x => x.InvoiceDetails).HasForeignKey(x => x.OfficeServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaintenanceRequest>(e =>
        {
            e.HasIndex(x => x.RequestCode).IsUnique();
            e.HasOne(x => x.Office).WithMany(x => x.MaintenanceRequests).HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany(x => x.MaintenanceRequests).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedEmployee).WithMany(x => x.AssignedRequests).HasForeignKey(x => x.AssignedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BankPaymentPending>(e =>
        {
            e.Property(x => x.ExpectedAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.InvoiceId, x.Status });
            e.HasIndex(x => x.PaymentReference);
            e.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
