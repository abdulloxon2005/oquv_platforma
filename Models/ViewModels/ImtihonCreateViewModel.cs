using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace talim_platforma.Models.ViewModels
{
    public class ImtihonCreateViewModel
    {
        [Required(ErrorMessage = "Guruh tanlanishi shart")]
        [Display(Name = "Guruh")]
        public int GuruhId { get; set; }

        [Required(ErrorMessage = "Imtihon nomi kiritilishi shart")]
        [Display(Name = "Imtihon nomi")]
        [StringLength(200)]
        public string Nomi { get; set; }

        [Required(ErrorMessage = "Imtihon turi tanlanishi shart")]
        [Display(Name = "Imtihon turi")]
        public string ImtihonTuri { get; set; } // haftalik, oylik, yakuniy

        [Required(ErrorMessage = "Imtihon formati tanlanishi shart")]
        [Display(Name = "Imtihon formati")]
        public string ImtihonFormati { get; set; } = "Offline"; // Online yoki Offline

        [Required(ErrorMessage = "Sana kiritilishi shart")]
        [Display(Name = "Sana")]
        [DataType(DataType.Date)]
        public DateTime Sana { get; set; } = DateTime.Now;

        [Display(Name = "Boshlanish vaqti")]
        public string? BoshlanishVaqti { get; set; }

        [Display(Name = "Tugash vaqti")]
        public string? TugashVaqti { get; set; }

        [Display(Name = "Muddat (daqiqa)")]
        [Range(1, 1000, ErrorMessage = "Muddat 1 dan 1000 daqiqagacha bo'lishi kerak")]
        public int? MuddatDaqiqada { get; set; }

        [Display(Name = "Minimal ball (%)")]
        [Range(0, 100, ErrorMessage = "Minimal ball 0 dan 100% gacha bo'lishi kerak")]
        public int MinimalBall { get; set; } = 60;

        [Display(Name = "Sertifikat beriladimi?")]
        public bool SertifikatBeriladimi { get; set; } = false;

        [Display(Name = "Izoh")]
        public string? Izoh { get; set; }

        // Select lists
        public List<SelectListItem> Guruhlar { get; set; } = new();
        public List<SelectListItem> ImtihonTurlari { get; set; } = new();
        public List<SelectListItem> ImtihonFormatlari { get; set; } = new();
    }
}


