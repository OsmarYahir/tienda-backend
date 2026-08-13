namespace Ticket.API.Exceptions
{
    // Token válido pero sin el rol/permiso requerido en Order.API (403), se relaya tal cual.
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}
