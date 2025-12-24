using System;
using System.Collections.Generic;

namespace talim_platforma.Models.ViewModels
{
    public class TeacherGroupCard
    {
        public int Id { get; set; }
        public string Nomi { get; set; } = string.Empty;
        public string KursNomi { get; set; } = string.Empty;
        public string DarsVaqti { get; set; } = string.Empty;
        public string DarsKunlari { get; set; } = string.Empty;
        public int TalabalarSoni { get; set; }
        public string Holati { get; set; } = string.Empty;
    }

    public class TeacherAttendanceSnapshot
    {
        public DateTime Sana { get; set; }
        public string GuruhNomi { get; set; } = string.Empty;
        public string TalabaFIO { get; set; } = string.Empty;
        public string Holati { get; set; } = string.Empty;
    }

    public class TeacherDashboardViewModel
    {
        public int GuruhlarSoni { get; set; }
        public int TalabalarSoni { get; set; }
        public int BugungiDarslar { get; set; }
        public int BugungiDavomatOlindi { get; set; }
        public List<TeacherGroupCard> Guruhlar { get; set; } = new();
        public List<TeacherAttendanceSnapshot> OxirgiDavomatlar { get; set; } = new();
    }
}



