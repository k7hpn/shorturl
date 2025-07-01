using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ShortURL.Data
{
    public class Lookup(ILogger<Lookup> logger, IDistributedCache cache, Context context)
    {
        private readonly IDistributedCache _cache = cache
            ?? throw new ArgumentNullException(nameof(cache));

        private readonly Context _context = context
            ?? throw new ArgumentNullException(nameof(context));

        private readonly ILogger _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        public static string GetCacheKey(string domain = null, string slug = null)
        {
            return !string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(slug)
                ? $"d.{domain}.s.{slug}"
                : !string.IsNullOrEmpty(domain)
                    ? $"d.{domain}"
                    : !string.IsNullOrEmpty(slug)
                        ? $"s.{slug}"
                        : "default";
        }

        public async Task<Model.IdAndLink> GetGroupDefaultAsync(string domainName)
        {
            string cacheKey = GetCacheKey(domain: domainName);

            var cachedValue = await GetCacheAsync(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var groupId = await _context.Domains
                .AsNoTracking()
                .Where(_ => _.Name == domainName)
                .Select(_ => _.GroupId)
                .SingleOrDefaultAsync();

            if (groupId == default)
            {
                _logger.LogInformation("No group with name equal to {domainName}",
                    domainName);
            }

            var idAndLink = await _context.Groups
                .AsNoTracking()
                .Where(_ => _.GroupId == groupId)
                .Select(_ => new Model.IdAndLink { Id = groupId, Link = _.DefaultLink })
                .SingleOrDefaultAsync();

            if (idAndLink != null)
            {
                await SetCacheAsync(cacheKey, idAndLink);
            }

            return idAndLink;
        }

        public async Task<Model.IdAndLink> GetGroupSlugAsync(string domainName, string slugText)
        {
            string cacheKey = GetCacheKey(domainName, slugText);

            var cachedValue = await GetCacheAsync(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var groupId = await _context.Domains
                .AsNoTracking()
                .Where(_ => _.Name == domainName)
                .Select(_ => _.GroupId)
                .SingleOrDefaultAsync();

            var idAndLink = await _context.Records
                .AsNoTracking()
                .Where(_ => _.IsActive
                    && _.GroupId == groupId
                    && _.Stub == slugText)
                .Select(_ => new Model.IdAndLink { Id = _.RecordId, Link = _.Link })
                .SingleOrDefaultAsync();

            if (idAndLink != null)
            {
                await SetCacheAsync(cacheKey, idAndLink);
            }

            return idAndLink;
        }

        public async Task<Model.IdAndLink> GetSlugNoGroupAsync(string slugText)
        {
            string cacheKey = GetCacheKey(slug: slugText);

            var cachedValue = await GetCacheAsync(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var idAndLink = await _context.Records
                .AsNoTracking()
                .Where(_ => _.IsActive
                    && _.GroupId == null
                    && _.Stub == slugText)
                .Select(_ => new Model.IdAndLink { Id = _.RecordId, Link = _.Link })
                .SingleOrDefaultAsync();

            if (idAndLink != null)
            {
                await SetCacheAsync(cacheKey, idAndLink);
            }

            return idAndLink;
        }

        public async Task<Model.IdAndLink> GetSystemDefault()
        {
            string cacheKey = GetCacheKey();

            var cachedValue = await GetCacheAsync(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var idAndLink = await _context.Groups
                .AsNoTracking()
                .Where(_ => _.IsDefault)
                .Select(_ => new Model.IdAndLink { Id = _.GroupId, Link = _.DefaultLink })
                .SingleOrDefaultAsync();

            if (idAndLink != null)
            {
                await SetCacheAsync(cacheKey, idAndLink);
            }

            return idAndLink;
        }

        private async Task<Model.IdAndLink> GetCacheAsync(string key)
        {
            var cacheValue = await _cache.GetStringAsync(key);
            if (cacheValue == null)
            {
                _logger.LogTrace("Cache miss for {Key}", key);
                return null;
            }

            Model.IdAndLink idAndLink = null;

            try
            {
                idAndLink = JsonSerializer.Deserialize<Model.IdAndLink>(cacheValue);
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex,
                    "Problem deserializing cached link, removing {Key} from cache: {ErrorMessage}",
                    key,
                    jex.Message);
                await _cache.RemoveAsync(key);
            }

            if (idAndLink == null)
            {
                _logger.LogWarning(
                    "Cache hit but removing {Key}, could not deserialize: {Serialized}",
                    key,
                    cacheValue);
                await _cache.RemoveAsync(key);
            }

            return idAndLink;
        }

        private async Task SetCacheAsync(string key, Model.IdAndLink idAndLink)
        {
            _logger.LogTrace("Setting cache for {CacheKey}: {Id}, {Link}",
                key,
                idAndLink.Id,
                idAndLink.Link);

            await _cache.SetStringAsync(key,
                JsonSerializer.Serialize(idAndLink), new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(360)
                });
        }
    }
}