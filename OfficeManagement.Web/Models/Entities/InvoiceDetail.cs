using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeManagement.Web.Models.Entities;

public class InvoiceDetail
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public int OfficeServiceId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PreviousReading { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CurrentReading { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public decimal Quantity => CurrentReading - PreviousReading;

    public decimal LineTotal => Quantity * UnitPrice;

    public Invoice Invoice { get; set; } = null!;
    public OfficeService OfficeService { get; set; } = null!;
}
