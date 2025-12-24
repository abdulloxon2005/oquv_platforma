using System;

namespace talim_platforma.Helpers
{
    public static class RoleHelper
    {
        public static string Normalize(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return "user";
            }

            return role.Trim().ToLowerInvariant() switch
            {
                "admin" => "admin",
                "teacher" or "oqituvchi" => "teacher",
                "talaba" or "student" or "oquvchi" => "student",
                _ => "user"
            };
        }

        public static bool IsAdmin(string? role) =>
            string.Equals(Normalize(role), "admin", StringComparison.OrdinalIgnoreCase);

        public static bool IsTeacher(string? role)
        {
            var normalized = Normalize(role);
            return normalized == "teacher" || normalized == "admin";
        }

        public static bool IsStudent(string? role) =>
            string.Equals(Normalize(role), "student", StringComparison.OrdinalIgnoreCase);
    }
}


