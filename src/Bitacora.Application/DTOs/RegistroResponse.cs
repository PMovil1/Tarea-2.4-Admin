namespace Bitacora.Application.DTOs;

public sealed record RegistroResponse(
    int Id,
    string Titulo,
    string Descripcion,
    bool TieneFoto,
    string? FotoBase64,
    DateTimeOffset FechaCreacion,
    bool EsPropio
);
