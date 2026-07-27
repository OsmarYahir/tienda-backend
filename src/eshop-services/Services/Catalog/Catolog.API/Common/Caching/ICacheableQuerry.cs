namespace Catalog.API.Common.Caching
{
    public interface ICacheableQuerry
    {
        string CacheKey { get; }

        TimeSpan Expiration { get; }
    }
}
