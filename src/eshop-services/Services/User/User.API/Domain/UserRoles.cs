namespace User.API.Domain
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";

        public static readonly string[] AllowedRoles = [Admin, Customer];

        public static bool IsValid(string role) => AllowedRoles.Contains(role);
    }
}
