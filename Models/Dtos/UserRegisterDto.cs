using System.ComponentModel.DataAnnotations;

namespace CreditCardApiSimple.Models.Dtos
{
    public class UserRegisterDto
    {
        [Required]
        [EmailAddress]
        public string UserEmail { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
