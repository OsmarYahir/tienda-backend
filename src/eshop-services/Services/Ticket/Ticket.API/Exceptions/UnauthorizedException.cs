namespace Ticket.API.Exceptions
{
    // Se lanza cuando Order.API rechaza la petición propagada por falta de token
    // (o token inválido/expirado). Ticket.API no valida el JWT por su cuenta:
    // Order.API sigue siendo la autoridad de autenticación, esto solo relaya su 401.
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
