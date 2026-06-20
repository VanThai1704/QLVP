namespace OfficeManagement.Web.Models.Constants;

public static class OfficeStatuses
{
    public const string Available = "Available";
    public const string Rented = "Rented";
    public const string Maintenance = "Maintenance";

    public static readonly string[] All = [Available, Rented, Maintenance];
}

public static class ContractStatuses
{
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Terminated = "Terminated";

    public static readonly string[] All = [Active, Expired, Terminated];
}

public static class InvoiceStatuses
{
    public const string Unpaid = "Unpaid";
    public const string PendingPayment = "PendingPayment";
    public const string Paid = "Paid";
    public const string Overdue = "Overdue";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Unpaid, PendingPayment, Paid, Overdue, Cancelled];

    public static readonly string[] AwaitingPayment = [Unpaid, Overdue, PendingPayment];
}

public static class MaintenanceStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, InProgress, Completed, Cancelled];
}

public static class MaintenancePriorities
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Urgent = "Urgent";

    public static readonly string[] All = [Low, Normal, High, Urgent];
}

public static class PaymentMethods
{
    public const string Cash = "Cash";
    public const string BankTransfer = "Bank Transfer";
    public const string CreditCard = "Credit Card";

    public static readonly string[] All = [Cash, BankTransfer];
}

public static class RentalRequestStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [Pending, Approved, Rejected];
}
