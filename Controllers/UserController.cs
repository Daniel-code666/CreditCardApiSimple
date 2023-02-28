using CreditCardApiSimple.Data;
using CreditCardApiSimple.Models;
using CreditCardApiSimple.Models.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace CreditCardApiSimple.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiExplorerSettings(GroupName = "users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private ApiResponse _apiResponse;

        public UserController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
            _apiResponse = new();
        }

        [HttpPost]
        public IActionResult Register([FromBody] UserRegisterDto userReg)
        {
            try
            {
                var userTest = _db.User.FirstOrDefault(u => u.UserEmail == userReg.UserEmail);

                if (!CheckEmail(userReg.UserEmail))
                {
                    var user = new User()
                    {
                        UserEmail = userReg.UserEmail,
                        UserName = userReg.UserName
                    };

                    byte[] passwordHash, passwordSalt;

                    CrearPasswordHash(userReg.Password, out passwordHash, out passwordSalt);

                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;

                    _db.User.Add(user);

                    Save();

                    _apiResponse.StatusCode = HttpStatusCode.OK;
                    _apiResponse.IsSuccess = true;
                    _apiResponse.Result = user;
                    return Ok(_apiResponse);
                }
                else
                {
                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("El email ya está registrado");
                    return BadRequest(_apiResponse);
                }
            } 
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _apiResponse);
            }
        }

        [HttpPost]
        public IActionResult Login([FromBody] UserLoginDto userLogin)
        {
            try
            {
                var loggedUser = _db.User.FirstOrDefault(u => u.UserEmail == userLogin.UserEmail);

                if (loggedUser == null)
                {
                    _apiResponse.StatusCode = HttpStatusCode.NotFound;
                    _apiResponse.IsSuccess = true;
                    _apiResponse.ErrorMessages.Add("Usuario no encontrado");
                    return NotFound(_apiResponse);
                }

                if (!VerificaPasswordHash(userLogin.Password, loggedUser.PasswordHash, loggedUser.PasswordSalt))
                {
                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.IsSuccess = true;
                    _apiResponse.ErrorMessages.Add("Contraseña incorrecta");
                    return BadRequest(_apiResponse);
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, loggedUser.UserId.ToString()),
                    new Claim(ClaimTypes.Name, loggedUser.UserEmail.ToString())
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Secret").Value));

                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.Now.AddDays(1),
                    SigningCredentials = credentials
                };

                var tokenHandler = new JwtSecurityTokenHandler();

                var token = tokenHandler.CreateToken(tokenDescriptor);

                var user = new UserLoginReponse
                {
                    UserName = loggedUser.UserName,
                    Token = tokenHandler.WriteToken(token)
                };

                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Result = user;
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

        private bool VerificaPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < hash.Length; i++)
                {
                    if (hash[i] != passwordHash[i]) return false;
                }
            }
            return true;
        }

        private void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool CheckEmail(string email)
        {
            return _db.User.Any(u => u.UserEmail == email);
        }

        private bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }
    }
}
