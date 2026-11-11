using Bitacora.Application.DTOs;
using Bitacora.Application.Interfaces;
using Bitacora.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bitacora.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthController(IUsuarioRepository usuarioRepository, ITokenService tokenService)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar([FromBody] RegisterRequestDto dto)
    {
        var email = dto.Email.Trim().ToUpperInvariant();
        if (await _usuarioRepository.ExistsAsync(email))
            return BadRequest(new { message = "El correo ya está registrado." });

        var usuario = new Usuario
        {
            Email = email,
            Role = "Usuario"
        };

        usuario.PasswordHash = _passwordHasher.HashPassword(usuario, dto.Password);

        try
        {
            await _usuarioRepository.AddAsync(usuario);
        }
        catch (CorreoDuplicadoException)
        {
            return BadRequest(new { message = "El correo ya está registrado." });
        }

        return Ok(new { message = "Usuario registrado exitosamente." });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var usuario = await _usuarioRepository.GetByEmailAsync(dto.Email.Trim().ToUpperInvariant());
        if (usuario == null)
            return Unauthorized(new { message = "Credenciales inválidas." });

        var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Credenciales inválidas." });

        var token = _tokenService.GenerateToken(usuario, out var expiration);

        return Ok(new AuthResponseDto(token, expiration, usuario.Role, usuario.Email));
    }
}