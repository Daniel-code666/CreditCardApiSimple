using System.ComponentModel.DataAnnotations;

namespace CreditCardApiSimple.Models.Dtos
{
    public class CreditCardDto
    {
        public int CardId { get; set; }

        public string CardNumber { get; set; }

        public string ExpiringDate { get; set; }

        public string CVV { get; set; }
    }
}
