namespace User.API.Exceptions
{
    // User.API es net8.0 (BuildingBlocks del resto de la solución es net9.0, no se puede
    // referenciar entre frameworks distintos), así que replica localmente el mismo patrón
    // de excepciones ya usado en Catalog/Basket/Order para mantener consistencia de estilo
    // entre microservicios sin acoplar sus ciclos de release.
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }
    }
}
