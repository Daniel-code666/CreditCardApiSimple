using System.ComponentModel.DataAnnotations;

namespace CreditCardApiSimple.Models.Dtos
{
    public class CreateCreditCard
    {
        [Required]
        public string CardNumber { get; set; }

        [Required]
        public string CVV { get; set; }
    }
}
