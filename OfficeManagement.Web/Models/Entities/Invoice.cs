using System.ComponentModel.DataAnnotations;

namespace OfficeManagement.Web.Models.Entities;

public class Invoice
{
    public int Id { get; set; }

    [Required, StringLength(10)]
    public string InvoiceCode { get; set; } = string.Empty;

    public int ContractId { get; set; }

    [Range(1, 12)]
    public byte BillingMonth { get; set; }

    [Range(2000, 9999)]
    public short BillingYear { get; set; }

    [DataType(DataType.Date)]
    public DateTime IssueDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal RentAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ServicesSubtotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = "Unpaid";

    [DataType(DataType.Date)]
    public DateTime? PaidDate { get; set; }

    [StringLength(30)]
    public string? PaymentMethod { get; set; }

    public Contract Contract { get; set; } = null!;
    public ICollection<InvoiceDetail> Details { get; set; } = [];

    public void RecalculateTotals()
    {
        ServicesSubtotal = Details.Sum(d => d.LineTotal);
        TotalAmount = RentAmount + ServicesSubtotal;
    }
}
