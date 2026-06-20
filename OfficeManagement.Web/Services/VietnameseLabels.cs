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

    public static string RentalRequestStatus(string? status) => status switch
    {
        "Pending" => "Chờ duyệt",
        "Approved" => "Đã duyệt",
        "Rejected" => "Từ chối",
        _ => status ?? string.Empty
    };

    public static string NumberToText(decimal amount)
    {
        long number = (long)Math.Round(amount, 0);
        if (number == 0) return "Không đồng";

        string[] units = { "", " nghìn", " triệu", " tỷ", " nghìn tỷ", " triệu tỷ" };
        string[] digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

        string ReadThreeDigits(int n, bool showZeroHundreds)
        {
            int hundreds = n / 100;
            int tens = (n % 100) / 10;
            int ones = n % 10;
            string res = "";

            if (hundreds > 0 || showZeroHundreds)
            {
                res += digits[hundreds] + " trăm ";
            }

            if (tens > 0)
            {
                if (tens == 1) res += "mười ";
                else res += digits[tens] + " mươi ";
            }
            else if (hundreds > 0 && ones > 0)
            {
                res += "lẻ ";
            }

            if (ones > 0)
            {
                if (ones == 1 && tens > 1) res += "mốt";
                else if (ones == 5 && tens > 0) res += "lăm";
                else res += digits[ones];
            }

            return res.Trim();
        }

        string result = "";
        int unitIndex = 0;

        while (number > 0)
        {
            int group = (int)(number % 1000);
            if (group > 0 || (number >= 1000 && number % 1000 > 0)) // Check to include units when mid-groups are non-zero
            {
                bool showZeroHundreds = number >= 1000;
                string groupText = ReadThreeDigits(group, showZeroHundreds);
                result = groupText + units[unitIndex] + " " + result;
            }
            number /= 1000;
            unitIndex++;
        }

        result = result.Trim();
        if (string.IsNullOrEmpty(result)) return "Không đồng";

        result = char.ToUpper(result[0]) + result[1..] + " đồng chẵn.";
        while (result.Contains("  ")) result = result.Replace("  ", " ");
        return result;
    }
}
