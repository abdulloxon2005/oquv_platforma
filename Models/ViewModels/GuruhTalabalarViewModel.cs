namespace talim_platforma.Models.ViewModels
{
    public class GuruhTalabalarViewModel
    {
        public int GuruhId { get; set; }
        public string GuruhNomi { get; set; }
        public string KursNomi { get; set; }
        public ICollection<TalabaStatusItem> Talabalar { get; set; } = new List<TalabaStatusItem>();
    }

    public class TalabaStatusItem
    {
        public int TalabaId { get; set; }
        public string Ism { get; set; }
        public string Familiya { get; set; }
        public string? Telefon { get; set; }
        public bool Faolmi { get; set; }
        public string? OxirgiHolat { get; set; }
        public DateTime? OxirgiSana { get; set; }
        public int? DavomatIdSababUchun { get; set; }
        public bool SababKiritishMumkin => DavomatIdSababUchun.HasValue;
    }
}

