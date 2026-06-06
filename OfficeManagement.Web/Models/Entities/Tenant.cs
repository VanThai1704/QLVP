using System.ComponentModel.DataAnnotations;

namespace OfficeManagement.Web.Models.Entities;

public class Tenant
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    [Required, StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string RepresentativeName { get; set; } = string.Empty;

    [StringLength(15)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = [];
}
