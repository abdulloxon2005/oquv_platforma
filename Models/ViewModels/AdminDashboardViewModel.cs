using talim_platforma.Models;

namespace talim_platforma.Models.ViewModels
{
    public class CoursePaymentStatus
    {
        public string KursNomi { get; set; } = string.Empty;
        public decimal KursNarxi { get; set; }
        public decimal Tolangan { get; set; }
        public decimal Qarzdorlik { get; set; }
        public decimal Haqdorlik { get; set; }
    }

    public class TalabaTolovHolatiViewModel
    {
        public Foydalanuvchi Talaba { get; set; }
        public List<CoursePaymentStatus> KursHolati { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalCourses { get; set; }
        public int ActiveGroups { get; set; }
        public int KursApplicationsCount { get; set; }
        public decimal TodaysIncome { get; set; }
        public decimal MonthlyIncome { get; set; }
        public int UpcomingExams { get; set; }
        public List<Tolov> RecentPayments { get; set; } = new();
        public List<Guruh> RecentGroups { get; set; } = new();
        public List<KursApplication> RecentKursApplications { get; set; } = new();
    }
}

