using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Bitacora.Api.Controllers;
using Bitacora.Application.DTOs;
using Bitacora.Application.Interfaces;
using Bitacora.Domain.Entities;
using Bitacora.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

static void Check(bool success, string name)
{
    if (!success) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}
// MVC validates record constructor parameter annotations, not property annotations.
static bool Valid<T>(T dto)
{
    var constructor = typeof(T).GetConstructors().Single();
    return constructor.GetParameters().All(p => p.GetCustomAttributes<ValidationAttribute>()
        .All(a => a.IsValid(typeof(T).GetProperty(p.Name!)!.GetValue(dto))));
}
Check(!Valid(new RegisterRequestDto("invalid", "12345678")), "Invalid email");
Check(!Valid(new RegisterRequestDto("a@example.com", "short")), "Short password");
Check(!Valid(new RegisterRequestDto("a@example.com", new string('x', 129))), "Long password");
Check(!Valid(new LoginRequestDto("", "")), "Missing login data");
Check(Valid(new RegisterRequestDto("a@example.com", "Password123!")), "Valid registration data");
var repo = new Users();
var secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
    ["JwtSettings:Secret"] = secret, ["JwtSettings:Issuer"] = "auth-checks",
    ["JwtSettings:Audience"] = "android-checks", ["JwtSettings:ExpiresInMinutes"] = "10"
}).Build();
var controller = new AuthController(repo, new JwtTokenService(config));
var request = JsonSerializer.Deserialize<RegisterRequestDto>("""{"Email":"user@example.com","Password":"Password123!","Role":"Admin"}""")!;
Check(await controller.Registrar(request) is OkObjectResult, "Register success");
Check(repo.User!.Role == "Usuario", "Client cannot register as Admin");
Check(repo.User.Email == "USER@EXAMPLE.COM", "Email normalized");
Check(repo.User.PasswordHash != request.Password && new PasswordHasher<Usuario>()
    .VerifyHashedPassword(repo.User, repo.User.PasswordHash, request.Password) != PasswordVerificationResult.Failed, "Stored password is a verifiable hash");
Check(await controller.Registrar(request) is BadRequestObjectResult, "Duplicate email rejected");
repo.DuplicateOnSave = true;
Check(await controller.Registrar(new("other@example.com", "Password123!")) is BadRequestObjectResult, "Concurrent duplicate mapped to 400");
repo.DuplicateOnSave = false;
var badPassword = (UnauthorizedObjectResult)await controller.Login(new("user@example.com", "wrong"));
var badEmail = (UnauthorizedObjectResult)await controller.Login(new("missing@example.com", "wrong"));
Check(JsonSerializer.Serialize(badPassword.Value) == JsonSerializer.Serialize(badEmail.Value), "Same 401 response for incorrect credentials");
var login = (OkObjectResult)await controller.Login(new("user@example.com", "Password123!"));
var response = (AuthResponseDto)login.Value!;
var handler = new JwtSecurityTokenHandler();
var principal = handler.ValidateToken(response.Token, new TokenValidationParameters {
    ValidateIssuer = true, ValidIssuer = "auth-checks", ValidateAudience = true, ValidAudience = "android-checks",
    ValidateLifetime = true, ClockSkew = TimeSpan.Zero, ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
}, out _);
Check(principal.FindFirstValue(ClaimTypes.Role) == "Usuario" && principal.FindFirstValue(ClaimTypes.NameIdentifier) == "1", "JWT signature, issuer, audience, lifetime and claims valid");
Check(response.Expiration > DateTime.UtcNow.AddMinutes(9) && response.Expiration <= DateTime.UtcNow.AddMinutes(10), "JWT expires in 10 minutes");
Console.WriteLine("All authentication checks passed. Repository is an in-memory test double; SQL and HTTP are not exercised.");

sealed class Users : IUsuarioRepository
{
    public Usuario? User;
    public bool DuplicateOnSave;
    public Task<bool> ExistsAsync(string email) => Task.FromResult(User?.Email == email);
    public Task<Usuario?> GetByEmailAsync(string email) => Task.FromResult(User?.Email == email ? User : null);
    public Task AddAsync(Usuario user)
    {
        if (DuplicateOnSave) throw new CorreoDuplicadoException(new Exception("Simulated duplicate"));
        user.Id = 1;
        User = user;
        return Task.CompletedTask;
    }
}
