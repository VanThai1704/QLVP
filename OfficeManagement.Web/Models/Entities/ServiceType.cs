using System.ComponentModel.DataAnnotations;

namespace OfficeManagement.Web.Models.Entities;

public class ServiceType
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal DefaultUnitPrice { get; set; }

    public bool IsMetered { get; set; } = true;

    public ICollection<OfficeService> OfficeServices { get; set; } = [];
}
