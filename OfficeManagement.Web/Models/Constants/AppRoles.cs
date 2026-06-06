namespace OfficeManagement.Web.Models.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Accountant = "Accountant";
    public const string Technician = "Technician";
    public const string Tenant = "Tenant";

    public static readonly string[] All = [Admin, Manager, Accountant, Technician, Tenant];

    public static readonly string[] Staff = [Admin, Manager, Accountant, Technician];
}
