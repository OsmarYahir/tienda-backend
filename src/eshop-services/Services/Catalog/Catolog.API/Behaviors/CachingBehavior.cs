using Catalog.API.Common.Caching;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.API.Behaviors
{
    // Cachea en memoria las queries que implementan ICacheableQuerry.
    // Antes solo existía la interfaz (CacheKey/Expiration) pero nada la consumía:
    // este behavior es lo que realmente conecta esa pieza al pipeline de MediatR.
    public class CachingBehavior<TRequest, TResponse>(
        IMemoryCache cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not ICacheableQuerry cacheableQuery)
                return await next();

            if (cache.TryGetValue(cacheableQuery.CacheKey, out TResponse? cachedResponse) && cachedResponse is not null)
            {
                logger.LogInformation("Cache hit para {CacheKey}", cacheableQuery.CacheKey);
                return cachedResponse;
            }

            logger.LogInformation("Cache miss para {CacheKey}", cacheableQuery.CacheKey);
            var response = await next();

            cache.Set(cacheableQuery.CacheKey, response, cacheableQuery.Expiration);
            return response;
        }
    }
}
