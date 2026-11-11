using System.ComponentModel.DataAnnotations;

namespace Bitacora.Application.DTOs;

public record RegisterRequestDto(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password);

public record LoginRequestDto(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MaxLength(128)] string Password);

public record AuthResponseDto(string Token, DateTime Expiration, string Role, string Email);
