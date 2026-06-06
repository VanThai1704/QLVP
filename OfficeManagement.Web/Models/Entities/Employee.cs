using System.ComponentModel.DataAnnotations;

namespace OfficeManagement.Web.Models.Entities;

public class Employee
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(15)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Position { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<Contract> CreatedContracts { get; set; } = [];
    public ICollection<MaintenanceRequest> AssignedRequests { get; set; } = [];
}
