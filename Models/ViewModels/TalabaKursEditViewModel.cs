using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace talim_platforma.Models
{
    /// <summary>
    /// Talaba-Kurs bog‘lanmasini tahrirlash uchun ViewModel
    /// </summary>
    public class TalabaKursEditViewModel
    {
        /// <summary>
        /// TalabaKurs jadvalidagi primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tanlangan talaba Id
        /// </summary>
        [Required(ErrorMessage = "Talaba tanlanishi shart")]
        [Display(Name = "Talaba")]
        public int TalabaId { get; set; }

        /// <summary>
        /// Tanlangan kurslar Id ro‘yxati
        /// </summary>
        [Required(ErrorMessage = "Kamida bitta kurs tanlanishi shart")]
        [Display(Name = "Kurslar")]
        public List<int> Kurslar { get; set; } = new List<int>();
    }
}
