using minimalAPILesson1.Data;
using minimalAPILesson1.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using minimalAPILesson1.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Numerics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace minimalAPILesson1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly AppDBContext _context; // Use the fully qualified name if AppDBContext is a class inside the AppDBContext namespace

        // Inject AppDbContext via Dependency Injection
        public PlayerController(AppDBContext context) // Update constructor parameter type as well
        {
            _context = context;
        }

        // POST: api/player/GetAllPlayers
        [HttpGet("GetAllPlayers")]
        public async Task<IActionResult> GetAllPlayers()
        {

            // 1. Manually check if the user is authenticated via JWT token
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new ResponseDTO<object>(
                    Message: "Unauthorized: Missing or invalid token",
                    StatusCode: 401,
                    Data: null
                ));
            }

            var players = await _context.Players.Select(p => new PlayerDTO(p.Id, p.Name, p.Email )).ToListAsync();

            if (players == null || players.Count == 0)
            {
                return NotFound(new ResponseDTO<List<PlayerDTO>>(
                    Message: "No players found",
                    StatusCode: 404,
                    Data: null
                ));
            }

            return Ok(new ResponseDTO<List<PlayerDTO>>(
                Message: "Players retrieved successfully",
                StatusCode: 200,
                Data: players
            ));
        }

        [HttpPatch("UpdatePlayer/{playerID}")]
        public async Task<IActionResult> UpdatePlayer(int playerID, UpdatePlayerDto updatePlayerDto)
        {
            var existingPlayer = await _context.Players.FindAsync(playerID);
            if (existingPlayer == null)
            {
                return NotFound(new ResponseDTO<Player>(
                    Message: "Player does not exist",
                    StatusCode: 404,
                    Data: null
                ));
            }

            updatePlayerDto.Adapt(existingPlayer);

            await _context.SaveChangesAsync();
            return Ok(new ResponseDTO<Player>(
                Message: "Successfully Updated Controller",
                StatusCode: 200,
                Data: existingPlayer
            ));
        }

        // POST: api/player
        [HttpPost("CreatePlayer")]
        public async Task<IActionResult> CreatePlayer(Player newPlayer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingPlayer = _context.Players.FirstOrDefault(p => p.Email == newPlayer.Email);
            if (existingPlayer != null)
            {
                return Conflict("A player with the same email already exists.");
            }

            //Password hashing first
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPlayer.Password);
            newPlayer.Password = hashedPassword;

            _context.Players.Add(newPlayer);
            await _context.SaveChangesAsync();
            return Ok(newPlayer);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO, [FromServices] IConfiguration configuration)
        {
            var existingPlayer = await _context.Players.FirstOrDefaultAsync(p => p.Email == loginDTO.Email);
            if (existingPlayer == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, existingPlayer.Password))
            {
                return Unauthorized(new ResponseDTO<Player>(
                    Message: "Invalid email or password",
                    StatusCode: 401,
                    Data: null
                ));
            }

            // 1. Generate the tokens using our helper methods
            var accessToken = GenerateAccessToken(existingPlayer, configuration);
            var refreshToken = GenerateRefreshToken();

            // 2. Save the Refresh Token and its expiry to the database
            existingPlayer.RefreshToken = refreshToken;
            existingPlayer.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Valid for 7 days
            await _context.SaveChangesAsync();

            // 3. Return the response
            return Ok(new ResponseDTO<LoginResponseDTO>(
                Message: "Login successful",
                StatusCode: 200,
                Data: new LoginResponseDTO(
                    ID: existingPlayer.Id,
                    ShortToken: accessToken,
                    LongToken: refreshToken,
                    Name: existingPlayer.Name,
                    Age: existingPlayer.Age,
                    Year: existingPlayer.Year,
                    Section: existingPlayer.Section,
                    Level: existingPlayer.Level
                )
            ));
        }

        // Generates the short-lived JWT Access Token
        public string GenerateAccessToken(Player player, IConfiguration configuration)
        {

            // 1. Define the claims (payload data embedded inside the token) NOTE: this to know which player this token belongs to
            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
        new Claim(ClaimTypes.Email, player.Email),
        new Claim(ClaimTypes.Name, player.Name)
    };

            // 2. Get your secret key from configuration (appsettings.json) and generate symmetric security key NOTE: this is like a password to confirm the token comes from our app, server confirms this 
            var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Create the token descriptor NOTE: tell token what is it for and who is it for
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15), // Short-lived (15 mins)
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                SigningCredentials = creds
            };

            // 4. Generate the actual token string NOTE: Generate the token itself the jwtTokenString holds the token itself
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Generates a random secure string for the Refresh Token (NOTE: this one saved in player database 
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        [HttpDelete("DeletePlayer/{playerID}")]
        public async Task<IActionResult> DeletePlayer(int playerID)
        {
            var findPlayer = await _context.Players.FindAsync(playerID);

            if(findPlayer == null)
            {
                return NotFound(new ResponseDTO<Player>(
                    Message: "Player does not exist",
                    StatusCode: 404,
                    Data: null
                    ));
            }

            _context.Players.Remove(findPlayer);
            await _context.SaveChangesAsync();

            return Ok(new ResponseDTO<Player>(Message: "Succesfully Deleted", StatusCode: 200, Data: null));
        }
    }
}
