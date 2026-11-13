using Bitacora.Domain.Entities;
using Microsoft.Win32;

namespace Bitacora.Application.Interfaces;

/// <summary>
/// Contrato que define las operaciones de acceso a los
/// registros de la bitácora de campo.
/// Permite separar la lógica de aplicación de la
/// implementación de la base de datos.
/// </summary>

public interface IRegistroRepository
{

    /// <summary>
    /// Obtiene los registros creados por el usuario indicado.
    /// </summary>
    /// <param name="usuarioId">Identificador del propietario.</param>
    /// <param name="cancellationToken">
    /// Permite cancelar la operación asíncrona.
    /// </param>
    /// <returns>
    /// Una colección de registros.
    /// </returns>

    Task<IEnumerable<Registro>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene el registro indicado si pertenece al usuario.
    /// </summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="usuarioId">Identificador del propietario.</param>
    /// <param name="cancellationToken">Permite cancelar la operación.</param>
    /// <returns>El registro propio o null si no hay coincidencia.</returns>
    Task<Registro?> ObtenerPorIdDelUsuarioAsync(int id, int usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Busca un registro por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="cancellationToken">
    /// Permite cancelar la operación.
    /// </param>
    /// <returns>
    /// El registro encontrado o null si no existe.
    /// </returns>

    Task<Registro?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Agrega un nuevo registro a la bitácora.
    /// </summary>
    /// <param name="registro">
    /// Entidad que contiene los datos del nuevo registro.
    /// </param>
    /// <param name="cancellationToken">
    /// Permite cancelar la operación.
    /// </param>

    Task CrearAsync(Registro registro, CancellationToken cancellationToken);

    /// <summary>
    /// Actualiza los datos de un registro existente.
    /// </summary>
    /// <param name="registro">
    /// Entidad que contiene los datos actualizados.
    /// </param>
    /// <param name="cancellationToken">
    /// Permite cancelar la operación.
    /// </param>

    Task ActualizarAsync(Registro registro, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina un registro existente de la bitácora.
    /// </summary>
    /// <param name="registro">
    /// Entidad que se desea eliminar.
    /// </param>
    /// <param name="cancellationToken">
    /// Permite cancelar la operación.
    /// </param>

    Task EliminarAsync(Registro registro, CancellationToken cancellationToken);
}
