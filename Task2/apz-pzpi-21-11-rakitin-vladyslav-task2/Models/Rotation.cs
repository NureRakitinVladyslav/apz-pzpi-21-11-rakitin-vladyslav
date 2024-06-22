using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace apz_backend.Models
{
    public class Rotation
    {
        // Основні атрибути
        [Key]
        public int Id { get; set; } // Ідентифікатор
        [Column(TypeName = "Date")]
        public DateTime LeaveDate { get; set; } // Дата відправлення
        [Column(TypeName = "Date")]
        public DateTime ReturnDate { get; set; } // Дата повернення
        public string Comment { get; set; } = string.Empty; // Коментарі (за замовчуванням порожній рядок)

        // Відносини
        public int SoldierId { get; set; } // Ідентифікатор солдата
        [ForeignKey("SoldierId")]
        public Soldier Soldier { get; set; } // Солдат, який перебуває у ротації
    }
}
