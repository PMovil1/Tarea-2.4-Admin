namespace Bitacora.Application.Interfaces;

public sealed class CorreoDuplicadoException(Exception innerException)
    : Exception("El correo ya está registrado.", innerException);
