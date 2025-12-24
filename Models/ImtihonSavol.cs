using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class ImtihonSavol
{
    public int Id { get; set; }
    public int ImtihonId { get; set; }

    public string SavolMatni { get; set; }
    public string VariantA { get; set; }
    public string VariantB { get; set; }
    public string VariantC { get; set; }
    public string VariantD { get; set; }
    public string TogriJavob { get; set; }
    public int BallQiymati { get; set; }

    public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
    public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

    public Imtihon Imtihon { get; set; }
}

}