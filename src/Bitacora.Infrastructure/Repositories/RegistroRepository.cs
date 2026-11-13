using Bitacora.Application.Interfaces;
using Bitacora.Domain.Entities;
using Bitacora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bitacora.Infrastructure.Repositories;

public sealed class RegistroRepository(ApplicationDbContext context) : IRegistroRepository
{
    public async Task<IEnumerable<Registro>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken)
    {
        return await context.Registros
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Registro?> ObtenerPorIdDelUsuarioAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        return await context.Registros
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task<Registro?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Registros
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task CrearAsync(Registro registro, CancellationToken cancellationToken)
    {
        await context.Registros.AddAsync(registro, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(Registro registro, CancellationToken cancellationToken)
    {
        context.Registros.Update(registro);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarAsync(Registro registro, CancellationToken cancellationToken)
    {
        context.Registros.Remove(registro);
        await context.SaveChangesAsync(cancellationToken);
    }
}
