using System.ComponentModel.DataAnnotations;

namespace OfficeManagement.Web.Models.Entities;

public class Contract
{
    public int Id { get; set; }

    [Required, StringLength(10)]
    public string ContractCode { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime SignedDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DepositAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MonthlyRent { get; set; }

    [StringLength(500)]
    public string? Terms { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = "Active";

    public int TenantId { get; set; }
    public int OfficeId { get; set; }
    public int? CreatedByEmployeeId { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Office Office { get; set; } = null!;
    public Employee? CreatedByEmployee { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = [];

    public bool IsActiveOn(DateTime date) =>
        Status == "Active" && date.Date >= StartDate.Date && date.Date <= EndDate.Date;
}
