using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace apz_backend.Models
{
    public class Request
    {
        // Основні атрибути
        [Key]
        public int Id { get; set; } // Ідентифікатор
        public DateTime Time { get; set; } // Час подання запиту
        public string Reason { get; set; } = string.Empty; // Причина запиту (за замовчуванням порожній рядок)
        public string? Text { get; set; } // Текст запиту (може бути null)

        // Відносини
        public int SoldierId { get; set; } // Ідентифікатор солдата
        [ForeignKey("SoldierId")]
        public Soldier Soldier { get; set; } // Солдат, який подав запит
    }
}