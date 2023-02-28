using CreditCardApiSimple.Data;
using CreditCardApiSimple.Models;
using CreditCardApiSimple.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;

namespace CreditCardApiSimple.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiExplorerSettings(GroupName = "creditcard")]
    [ApiController]
    [Authorize]
    public class CrediCardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private ApiResponse _apiResponse;
        private string _secretKey;

        public CrediCardController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
            _apiResponse = new();
            _secretKey = _config.GetValue<string>("AppSettings:Secret");
        }

        [HttpPost]
        public IActionResult CreateCrediCard([FromBody] CreateCreditCard createdCard)
        {
            try
            {
                var UserEmail = GetEmailFromToken();

                if (!ModelState.IsValid || createdCard == null || string.IsNullOrEmpty(UserEmail))
                {
                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("Faltan datos");
                    return BadRequest(_apiResponse);
                }

                var user = _db.User.FirstOrDefault(u => u.UserEmail == UserEmail);

                if (user == null)
                {
                    _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("Usuario imposible de autenticar");
                    return Unauthorized(_apiResponse);
                }

                if (_db.CreditCard.Any(c => c.CVV == createdCard.CVV || c.CardNumber == createdCard.CardNumber))
                {
                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("Ya hay una tarjeta con los mismos datos");
                    return BadRequest(_apiResponse);
                }

                var card = new CreditCard()
                {
                    CardNumber = createdCard.CardNumber,
                    CVV = createdCard.CVV,
                    ExpiringDate = DateTime.Now.AddYears(4).ToString(),
                    UserId = user.UserId
                };

                _db.CreditCard.Add(card);

                Save();

                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Result = card;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _apiResponse);
            }
        }

        [HttpGet]
        public IActionResult GetCreditCardsFromUser()
        {
            try
            {
                var UserEmail = GetEmailFromToken();

                if (string.IsNullOrEmpty(UserEmail))
                {
                    _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("No hay usuario con ese email");
                    return BadRequest(_apiResponse);
                }

                var cards = GetCreditCardsFromUser(UserEmail);

                if (cards.Status != "true")
                {
                    _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add(cards.Status);
                    return StatusCode(500, _apiResponse);
                }

                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Result = cards;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _apiResponse);
            }
        }

        [HttpDelete("{CardId}")]
        public IActionResult DeleteCard([FromRoute] int CardId)
        {
            try
            {
                int UserId = GetIdFromToken();

                if (UserId == 0)
                {
                    _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("El id del usuario es erróneo");
                    return Unauthorized(_apiResponse);
                }

                var deletedCard = deleteCard(CardId, UserId);

                if (deletedCard == null)
                {
                    _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("El id de la tarjeta es incorrecto");
                    return Unauthorized(_apiResponse);
                }

                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Result = deletedCard;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _apiResponse);
            }
        }

        private bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }

        private CreditCardDto deleteCard(int CardId, int UserId)
        {
            try
            {
                var card = _db.CreditCard.FirstOrDefault(c => c.CardId == CardId && c.UserId == UserId);

                if (card == null) return null;

                _db.CreditCard.Remove(card);

                Save();

                return new CreditCardDto()
                {
                    CardId = card.CardId,
                    CardNumber = card.CardNumber,
                    CVV = card.CVV,
                    ExpiringDate = card.ExpiringDate
                };
            }
            catch (Exception ex)
            {
                return new CreditCardDto() { CardNumber = ex.Message };
            }
        }

        private UserCreditCard GetCreditCardsFromUser(string UserEmail)
        {
            try
            {
                var userCreditCard = new UserCreditCard();

                var cardListResult = _db.CreditCard.Join(_db.User, c => c.UserId, u => u.UserId, (creditcard, user) => new
                {
                    creditcard,
                    user
                }).Where(u => u.user.UserEmail == UserEmail);

                foreach (var item in cardListResult)
                {
                    var card = new CreditCardDto()
                    {
                        CardId = item.creditcard.CardId,
                        CardNumber = item.creditcard.CardNumber,
                        CVV = item.creditcard.CVV,
                        ExpiringDate = item.creditcard.ExpiringDate
                    };

                    userCreditCard.CardList.Add(card);
                    userCreditCard.User = item.user;
                }

                userCreditCard.Status = "true";

                return userCreditCard;
            }
            catch (Exception ex)
            {
                return new UserCreditCard() { Status = ex.Message };
            }
        }

        private int GetIdFromToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config.GetValue<string>("AppSettings:Secret"));
            var token = HttpContext.Request.Headers["Authorization"];

            token = token.ToString().Substring(7);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var UserId = jwtToken.Claims.First(x => x.Type == "nameid").Value;

            return int.Parse(UserId);
        }

        private string GetEmailFromToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config.GetValue<string>("AppSettings:Secret"));
            var token = HttpContext.Request.Headers["Authorization"];

            token = token.ToString().Substring(7);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var UserEmail = jwtToken.Claims.First(x => x.Type == "unique_name").Value;

            return UserEmail;
        }
    }
}
