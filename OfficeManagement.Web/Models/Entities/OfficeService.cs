using System.ComponentModel.DataAnnotations;

namespace OfficeManagement.Web.Models.Entities;

public class OfficeService
{
    public int Id { get; set; }

    public int OfficeId { get; set; }
    public int ServiceTypeId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public Office Office { get; set; } = null!;
    public ServiceType ServiceType { get; set; } = null!;
    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
}
