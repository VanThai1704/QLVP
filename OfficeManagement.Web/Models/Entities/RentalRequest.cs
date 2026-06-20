using System.ComponentModel.DataAnnotations;

namespace OfficeManagement.Web.Models.Entities;

public class RentalRequest
{
    public int Id { get; set; }

    [Required]
    public int OfficeId { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = "Pending";

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Office Office { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
