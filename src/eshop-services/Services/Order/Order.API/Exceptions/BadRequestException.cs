namespace Order.API.Exceptions
{
    // Order.API es net8.0 (BuildingBlocks es net9.0, por lo que no puede referenciarlo
    // como ProjectReference sin romper la compilación). Se replica aquí, deliberadamente,
    // el mismo patrón de excepciones de negocio ya usado en Catalog/Basket para mantener
    // consistencia de estilo entre microservicios sin acoplar sus ciclos de release.
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }
    }
}
