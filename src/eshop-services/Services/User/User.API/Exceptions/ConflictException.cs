namespace User.API.Exceptions
{
    // Recurso que ya existe (ej. email duplicado en /register) → 409, no 400:
    // el body de la petición es válido, el conflicto es con el estado actual del servidor.
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}
