using System;
using System.Collections.Generic;

namespace talim_platforma.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string FullName { get; set; } = "Talaba";
        public int FaolKurslar { get; set; }
        public int FaolGuruhlar { get; set; }
        public decimal JamiTolov { get; set; }
        public decimal Qarzdorlik { get; set; }
        public decimal Haqdorlik { get; set; }
        public decimal? OxirgiFoiz { get; set; }

        public List<StudentPaymentItem> Tolovlar { get; set; } = new();
        public List<StudentAttendanceItem> Davomatlar { get; set; } = new();
        public List<StudentExamCard> OnlineImtihonlar { get; set; } = new();
        public List<StudentExamResultItem> Natijalar { get; set; } = new();
        public List<StudentGroupCard> Guruhlar { get; set; } = new();
        public List<StudentCourseItem> Kurslar { get; set; } = new();
    }

    public class StudentPaymentItem
    {
        public DateTime Sana { get; set; }
        public decimal Miqdor { get; set; }
        public string Kurs { get; set; } = string.Empty;
        public string TolovUsuli { get; set; } = string.Empty;
        public decimal Qarzdorlik { get; set; }
        public decimal Haqdorlik { get; set; }
        public string ChekRaqami { get; set; } = string.Empty;
        public string Holat { get; set; } = string.Empty;
    }

    public class StudentAttendanceItem
    {
        public DateTime Sana { get; set; }
        public string Holati { get; set; } = string.Empty;
        public string Guruh { get; set; } = string.Empty;
    }

    public class StudentExamCard
    {
        public int Id { get; set; }
        public string Nomi { get; set; } = string.Empty;
        public string Guruh { get; set; } = string.Empty;
        public string Kurs { get; set; } = string.Empty;
        public DateTime Sana { get; set; }
        public int? MuddatDaqiqada { get; set; }
        public bool IsEligible { get; set; }
        public string EligibilityMessage { get; set; } = string.Empty;
    }

    public class StudentExamResultItem
    {
        public int NatijaId { get; set; }
        public string ImtihonNomi { get; set; } = string.Empty;
        public string Guruh { get; set; } = string.Empty;
        public DateTime Sana { get; set; }
        public decimal Foiz { get; set; }
        public bool Otdimi { get; set; }
    }

    public class StudentGroupCard
    {
        public int Id { get; set; }
        public string Nomi { get; set; } = string.Empty;
        public string Kurs { get; set; } = string.Empty;
        public string DarsVaqti { get; set; } = string.Empty;
        public string DarsKunlari { get; set; } = string.Empty;
        public int TalabalarSoni { get; set; }
    }

    public class StudentCourseItem
    {
        public string Nomi { get; set; } = string.Empty;
        public string Holati { get; set; } = "Faol";
        public decimal Narx { get; set; }
    }
}

