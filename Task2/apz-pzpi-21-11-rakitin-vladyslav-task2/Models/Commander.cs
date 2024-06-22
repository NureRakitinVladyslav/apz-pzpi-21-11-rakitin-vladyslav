using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace apz_backend.Models
{
    public class Commander
    {
        // Основні атрибути
        [Key]
        public int Id { get; set; } // Ідентифікатор
        public string UserName { get; set; } = string.Empty; // Ім'я користувача (за замовчуванням порожній рядок)
        public string UserPassword { get; set; } = string.Empty; // Пароль користувача (за замовчуванням порожній рядок)
        public string FullName { get; set; } = string.Empty; // Повне ім'я (за замовчуванням порожній рядок)

        [Column(TypeName = "Date")]
        public DateTime DateOfBirth { get; set; } // Дата народження
        public string Rank { get; set; } = string.Empty; // Звання (за замовчуванням порожній рядок)
        public string? PhotoURL { get; set; } // URL фотографії (може бути null)

        // Відносини
        public int UnitId { get; set; } // Ідентифікатор військової частини
        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } // Військова частина, яку командує командир
        public ICollection<Soldier>? Soldiers { get; set; } // Солдати, які підпорядковані командиру (може бути null)
    }
}