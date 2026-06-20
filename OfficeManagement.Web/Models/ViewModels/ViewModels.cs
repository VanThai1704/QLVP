using System.ComponentModel.DataAnnotations;
using OfficeManagement.Web.Models.Constants;
using OfficeManagement.Web.Models.Entities;

namespace OfficeManagement.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50)]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên công ty")]
    [StringLength(100)]
    [Display(Name = "Tên công ty")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập người đại diện")]
    [StringLength(100)]
    [Display(Name = "Người đại diện")]
    public string RepresentativeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [StringLength(15)]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (10–11 chữ số, bắt đầu bằng 0)")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    [StringLength(200)]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = string.Empty;
}

public class TenantCreateViewModel : RegisterViewModel { }

public class ProfileViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Họ tên / Công ty")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(200)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class EmployeeCreateViewModel
{
    [Required]
    [StringLength(50)]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "Chức vụ")]
    public string Position { get; set; } = string.Empty;
}

public class AccountManageViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LinkedName { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class DashboardViewModel
{
    public int TotalOffices { get; set; }
    public int AvailableOffices { get; set; }
    public int RentedOffices { get; set; }
    public int MaintenanceOffices { get; set; }
    public int ActiveContracts { get; set; }
    public int PendingRepairs { get; set; }
    public int UnpaidInvoices { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<RevenueStatItem> RevenueByMonth { get; set; } = [];
}

public class RevenueStatItem
{
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ContractCreateViewModel
{
    [Required, StringLength(10)]
    public string ContractCode { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateTime SignedDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

    [Range(0, double.MaxValue)]
    public decimal DepositAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MonthlyRent { get; set; }

    [StringLength(500)]
    public string? Terms { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required]
    public int OfficeId { get; set; }

    public List<int> SelectedServiceTypeIds { get; set; } = [];
}

public class InvoiceCreateViewModel
{
    [Required, StringLength(10)]
    public string InvoiceCode { get; set; } = string.Empty;

    [Required]
    public int ContractId { get; set; }

    [Range(1, 12)]
    public byte BillingMonth { get; set; } = (byte)DateTime.Today.Month;

    [Range(2000, 9999)]
    public short BillingYear { get; set; } = (short)DateTime.Today.Year;

    [DataType(DataType.Date)]
    public DateTime IssueDate { get; set; } = DateTime.Today;

    public List<InvoiceLineInput> Lines { get; set; } = [];
}

public class InvoiceLineInput
{
    public int OfficeServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public bool IsMetered { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
}

public class PaymentViewModel
{
    public int InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = PaymentMethods.BankTransfer;
}

public class InvoiceIndexViewModel
{
    [Display(Name = "Trạng thái")]
    public string? Status { get; set; }

    [Display(Name = "Tên khách hàng")]
    public string? TenantName { get; set; }

    public List<Invoice> Invoices { get; set; } = [];
}

public class BankTransferViewModel
{
    public int InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public string? QrCodeImageUrl { get; set; }
    public string QrCodeDataUri { get; set; } = string.Empty;
}

public class AssignTechnicianViewModel
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;

    [Required]
    public int AssignedEmployeeId { get; set; }
}

public class UpdateRepairViewModel
{
    public int Id { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? CompletedDate { get; set; }
}
