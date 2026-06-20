using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeManagement.Web.Models.Entities;

public class Office
{
    public int Id { get; set; }

    [Required, StringLength(10)]
    public string OfficeCode { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Số phòng")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal AreaSqm { get; set; }

    [Range(1, 500, ErrorMessage = "Sức chứa phải từ 1 đến 500 người")]
    [Display(Name = "Sức chứa (người)")]
    public int Capacity { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MonthlyRent { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = "Available";

    [StringLength(500)]
    public string? Description { get; set; }

    [NotMapped]
    public string DisplayLabel => $"{Name} · Phòng {RoomNumber} · {Capacity} người";

    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<OfficeService> OfficeServices { get; set; } = [];
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = [];
    public ICollection<RentalRequest> RentalRequests { get; set; } = [];
}
