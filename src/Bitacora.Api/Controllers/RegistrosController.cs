using System.Security.Claims;
using Bitacora.Application.DTOs;
using Bitacora.Application.Interfaces;
using Bitacora.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bitacora.Api.Controllers;

[Authorize] // Exige autenticación por defecto para todos los endpoints del controlador
[ApiController]
[Route("api/registros")]
public sealed class RegistrosController(IRegistroRepository repository) : ControllerBase
{
    // GET: /api/registros (Listar registros propios)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegistroResponse>>> ObtenerTodos(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized(new { message = "Token inválido o usuario no identificado." });
        }

        var registros = await repository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = registros.Select(r => new RegistroResponse(
            r.Id,
            r.Titulo,
            r.Descripcion,
            !string.IsNullOrEmpty(r.FotoBase64),
            r.FotoBase64,
            r.FechaCreacion,
            r.UsuarioId.ToString() == userId
        ));

        return Ok(response);
    }

    // GET: /api/registros/{id} (Consultar un registro propio con su foto)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RegistroResponse>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized(new { message = "Token inválido o usuario no identificado." });
        }

        var r = await repository.ObtenerPorIdDelUsuarioAsync(id, usuarioId, cancellationToken);

        if (r == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = new RegistroResponse(
            r.Id,
            r.Titulo,
            r.Descripcion,
            !string.IsNullOrEmpty(r.FotoBase64),
            r.FotoBase64,
            r.FechaCreacion,
            r.UsuarioId.ToString() == userId
        );

        return Ok(response);
    }

    // POST: /api/registros (Crear un registro asignando el usuario autenticado)
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearRegistroRequest request, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Token inválido o usuario no identificado." });
        }

        var registro = new Registro
        {
            Titulo = request.Titulo,
            Descripcion = request.Descripcion,
            FotoBase64 = request.FotoBase64,
            FechaCreacion = DateTimeOffset.UtcNow, // Asignada por el servidor
            UsuarioId = userId // Se enlaza con el usuario logueado
        };

        await repository.CrearAsync(registro, cancellationToken);

        var response = new RegistroResponse(
            registro.Id,
            registro.Titulo,
            registro.Descripcion,
            !string.IsNullOrEmpty(registro.FotoBase64),
            registro.FotoBase64,
            registro.FechaCreacion,
            true
        );

        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.Id }, response);
    }

    // PUT: /api/registros/{id} (Actualizar con validación de propiedad)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarRegistroRequest request, CancellationToken cancellationToken)
    {
        var existente = await repository.ObtenerPorIdAsync(id, cancellationToken);
        if (existente == null)
        {
            return NotFound();
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Token inválido o usuario no identificado." });
        }

        // Regla de negocio: Si no es Admin y no es el dueño del registro, retornar 403 Forbidden
        if (userRole != "Admin" && existente.UsuarioId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permisos para modificar un registro que no te pertenece." });
        }

        existente.Titulo = request.Titulo;
        existente.Descripcion = request.Descripcion;
        existente.FotoBase64 = request.FotoBase64;

        await repository.ActualizarAsync(existente, cancellationToken);

        return NoContent(); // 204 No Content
    }

    // DELETE: /api/registros/{id} (Eliminar solo permitido para administradores)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var existente = await repository.ObtenerPorIdAsync(id, cancellationToken);
        if (existente == null)
        {
            return NotFound();
        }

        await repository.EliminarAsync(existente, cancellationToken);

        return NoContent(); // 204 No Content
    }
}
