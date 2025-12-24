using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace talim_platforma.Models
{
    public class Baho
{
    public int Id { get; set; }
    public int? DarsId { get; set; }
    public int? ImtihonId { get; set; }
    public int TalabaId { get; set; }

    public int Ball { get; set; }
    public string Izoh { get; set; }

    public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
    public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

    public Dars Dars { get; set; }
    public Imtihon Imtihon { get; set; }
    public Foydalanuvchi Talaba { get; set; }
}
}