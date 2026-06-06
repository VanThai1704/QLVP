namespace OfficeManagement.Web.Services;

public static class VietnameseLabels
{
    public static string Role(string? role) => role switch
    {
        "Admin" => "Quản trị viên",
        "Manager" => "Quản lý tòa nhà",
        "Accountant" => "Kế toán",
        "Technician" => "Kỹ thuật viên",
        "Tenant" => "Khách thuê",
        _ => role ?? string.Empty
    };

    public static string OfficeStatus(string? status) => status switch
    {
        "Available" => "Còn trống",
        "Rented" => "Đang thuê",
        "Maintenance" => "Bảo trì",
        _ => status ?? string.Empty
    };

    public static string ContractStatus(string? status) => status switch
    {
        "Active" => "Đang hiệu lực",
        "Expired" => "Hết hạn",
        "Terminated" => "Đã kết thúc",
        _ => status ?? string.Empty
    };

    public static string InvoiceStatus(string? status) => status switch
    {
        "Unpaid" => "Chưa thanh toán",
        "PendingPayment" => "Đang chờ thanh toán",
        "Paid" => "Đã thanh toán",
        "Overdue" => "Quá hạn",
        "Cancelled" => "Đã hủy",
        _ => status ?? string.Empty
    };

    public static string MaintenanceStatus(string? status) => status switch
    {
        "Pending" => "Chờ xử lý",
        "InProgress" => "Đang xử lý",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => status ?? string.Empty
    };

    public static string Priority(string? priority) => priority switch
    {
        "Low" => "Thấp",
        "Normal" => "Bình thường",
        "High" => "Cao",
        "Urgent" => "Khẩn cấp",
        _ => priority ?? string.Empty
    };

    public static string PaymentMethod(string? method) => method switch
    {
        "Cash" => "Tiền mặt",
        "Bank Transfer" => "Chuyển khoản",
        "Credit Card" => "Thẻ tín dụng",
        _ => method ?? string.Empty
    };

    public static string AccountStatus(string? status) => status switch
    {
        "Active" => "Hoạt động",
        "Inactive" => "Ngưng hoạt động",
        "Locked" => "Đã khóa",
        _ => status ?? string.Empty
    };

    public static string ServiceName(string? name) => name switch
    {
        "Electricity" => "Điện",
        "Water" => "Nước",
        "Internet" => "Internet",
        "Cleaning" => "Vệ sinh",
        "Security" => "An ninh",
        _ => name ?? string.Empty
    };
}
