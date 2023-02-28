namespace CreditCardApiSimple.Models.Dtos
{
    public class UserCreditCard
    {
        public UserCreditCard()
        {
            CardList = new List<CreditCardDto>();
        }

        public User User { get; set; }

        public List<CreditCardDto> CardList { get; set; }

        public string Status { get; set; }
    }
}
