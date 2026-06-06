using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OfficeManagement.Web.Models.Entities;

public class MaintenanceRequest
{
    public int Id { get; set; }

    [StringLength(10)]
    public string RequestCode { get; set; } = string.Empty;

    public int OfficeId { get; set; }
    public int TenantId { get; set; }
    public int? AssignedEmployeeId { get; set; }

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Priority { get; set; } = "Normal";

    [StringLength(30)]
    public string Status { get; set; } = "Pending";

    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? CompletedDate { get; set; }

    [ValidateNever]
    public Office Office { get; set; } = null!;

    [ValidateNever]
    public Tenant Tenant { get; set; } = null!;

    [ValidateNever]
    public Employee? AssignedEmployee { get; set; }
}
