using BuildingBlocks.Exceptions;

namespace Basket.API.Exeptions
{
    public class BasketNotFoundException : NotFoundException 
    {
        public BasketNotFoundException(string userName): base("Basket", userName) 
        {
        
        }
    }
}
