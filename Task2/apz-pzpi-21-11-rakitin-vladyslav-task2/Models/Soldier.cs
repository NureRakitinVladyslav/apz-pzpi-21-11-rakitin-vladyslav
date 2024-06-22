using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace apz_backend.Models
{
    public class Soldier
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
        [Column(TypeName = "Date")]
        public DateTime EnlistDate { get; set; } // Дата призову
        [Column(TypeName = "Date")]
        public DateTime? DischargeDate { get; set; } // Дата звільнення (може бути null)

        // Відносини
        public int CommanderId { get; set; } // Ідентифікатор командира
        [ForeignKey("CommanderId")]
        public Commander Commander { get; set; } // Командир, під яким служить солдат
        public ICollection<Sleep>? Sleeps { get; set; } // Відпочинок солдата (може бути null)
        public ICollection<Rotation>? Rotations { get; set; } // Ротації солдата (може бути null)
        public ICollection<Request>? Requests { get; set; } // Запити солдата (може бути null)
    }
}