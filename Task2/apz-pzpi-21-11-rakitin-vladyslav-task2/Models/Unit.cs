using System.ComponentModel.DataAnnotations;

namespace apz_backend.Models
{
    public class Unit
    {
        // Основні атрибути
        [Key]
        public int Id { get; set; } // Ідентифікатор
        public int Number { get; set; } // Номер
        public string Type { get; set; } = string.Empty; // Тип (за замовчуванням порожній рядок)
        public string? Name { get; set; } // Назва (може бути null)
        public string Location { get; set; } = string.Empty; // Місцезнаходження (за замовчуванням порожній рядок)
        public string? Flag { get; set; } // Прапорець (може бути null)

        // Відносини
        public ICollection<Commander>? Commanders { get; set; } // Командири (може бути null)
    }
}
