using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace apz_backend.Models
{
    public class Sleep
    {
        // Основні атрибути
        [Key]
        public int Id { get; set; } // Ідентифікатор
        public DateTime StartTime { get; set; } // Початок сну
        public DateTime EndTime { get; set; } // Кінець сну
        public int? Quality { get; set; } // Якість сну (може бути null)

        // Відносини
        public int SoldierId { get; set; } // Ідентифікатор солдата
        [ForeignKey("SoldierId")]
        public Soldier Soldier { get; set; } // Солдат, який спить
    }
}
