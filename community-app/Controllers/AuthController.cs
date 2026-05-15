using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using community_app.Data;
using community_app.Models;
using community_app.DTOs;
using community_app.Services;

namespace community_app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already exists");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Role = "User"
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var result = _passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid credentials");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:DurationInMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDto dto,
            [FromServices] EmailService emailService)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Ok("If this email exists, a reset code has been sent.");

            user.ResetToken = Random.Shared.Next(100000, 999999).ToString();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            string subject = "Password Reset Code - Community App";
            
            string body = $@"
            <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f9; margin: 0; padding: 20px;"">
                <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.05);"">
                    <div style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;"">
                        <h1 style=""color: white; margin: 0; font-size: 28px; font-weight: 600; letter-spacing: -0.5px;"">Community App</h1>
                    </div>
                    <div style=""padding: 40px; text-align: center;"">
                        <h2 style=""color: #2d3748; margin-top: 0; font-size: 22px;"">Verification Code</h2>
                        <p style=""color: #4a5568; font-size: 16px; line-height: 1.6; margin-bottom: 30px;"">
                            Hello <strong>{user.Username}</strong>,<br><br>
                            We received a request to reset your password. Use the verification code below to set up a new one:
                        </p>
                        <div style=""display: inline-block; padding: 16px 40px; background-color: #f8fafc; border: 2px dashed #667eea; color: #667eea; border-radius: 12px; font-weight: 700; font-size: 32px; letter-spacing: 5px; margin: 10px 0;"">
                            {user.ResetToken}
                        </div>
                        <p style=""color: #718096; font-size: 14px; margin-top: 35px; border-top: 1px solid #edf2f7; padding-top: 20px;"">
                            Enter this code on the reset password page. If you didn't request this change, you can safely ignore this email. This code will expire in 1 hour.
                        </p>
                    </div>
                    <div style=""background-color: #f8fafc; padding: 20px; text-align: center; color: #a0aec0; font-size: 12px;"">
                        <p style=""margin: 5px 0;"">&copy; 2026 Community App. Built with passion for the community.</p>
                    </div>
                </div>
            </div>";

            await emailService.SendEmailAsync(user.Email, subject, body);

            return Ok("If this email exists, a reset code has been sent.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.ResetToken == dto.Token && u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
                return BadRequest("Invalid or expired token.");

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            return Ok("Password has been reset successfully.");
        }
    }
}