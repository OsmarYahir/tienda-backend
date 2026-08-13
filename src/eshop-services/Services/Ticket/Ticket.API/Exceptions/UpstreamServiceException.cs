namespace Ticket.API.Exceptions
{
    // Order.API respondió, pero con un error inesperado (5xx) o no respondió en absoluto.
    // Se traduce a 502 Bad Gateway: el problema es de un servicio "aguas arriba", no de
    // la petición del cliente ni de Ticket.API en sí.
    public class UpstreamServiceException : Exception
    {
        public UpstreamServiceException(string message) : base(message)
        {
        }
    }
}
