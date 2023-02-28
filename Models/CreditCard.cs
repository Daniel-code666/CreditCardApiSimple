using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CreditCardApiSimple.Models
{
    public class CreditCard
    {
        [Key]
        public int CardId { get; set; }

        [Required]
        public string CardNumber { get; set; }

        public string? ExpiringDate { get; set; }

        [Required]
        public string CVV { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }

        public User User { get; set; }
    }
}
