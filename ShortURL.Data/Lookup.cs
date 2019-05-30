using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ShortURL.Data
{
    public class Lookup
    {
        private readonly ILogger _logger;
        private readonly IDistributedCache _cache;
        private readonly Context _context;

        public Lookup(ILogger<Lookup> logger, IDistributedCache cache, Context context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private async Task<Model.IdAndLink> GetCacheAsync(string key)
        {
            var cacheValue = await _cache.GetAsync(key);
            if (cacheValue == null)
            {
                _logger.LogInformation("Cache miss for {Key}", key);
                return null;
            }

            using (var memoryStream = new MemoryStream(cacheValue))
            {
                var idAndLink = new BinaryFormatter().Deserialize(memoryStream) as Model.IdAndLink;
                if (idAndLink == null)
                {
                    _logger.LogWarning("Cache hit for {Key} but couldn't be converted to id and link", key);
                }
                else
                {
                    _logger.LogInformation("Cache hit for {Key}", key);
                }
                return idAndLink;
            }
        }

        private async Task SetCacheAsync(string key, Model.IdAndLink idAndLink)
        {
            _logger.LogInformation("Setting cache for {CacheKey}: {Id}, {Link}",
                key,
                idAndLink.Id,
                idAndLink.Link);

            using (var memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memoryStream, idAndLink);
                await _cache.SetAsync(key, memoryStream.ToArray(), new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(120)
                });
            }
        }

        public async Task<Model.IdAndLink> GetGroupStubAsync(string domainName, string stubText)
        {
            string cacheKey = GetCacheKey(domainName, stubText);

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
                    domainName,
                    stubText);
            }

            var idAndLink = await _context.Records
                .AsNoTracking()
                .Where(_ => _.IsActive
                    && _.GroupId == groupId
                    && _.Stub == stubText)
                .Select(_ => new Model.IdAndLink { Id = _.RecordId, Link = _.Link })
                .SingleOrDefaultAsync();

            if (idAndLink != null)
            {
                await SetCacheAsync(cacheKey, idAndLink);
            }
            else
            {
                _logger.LogInformation("No match for domain {domainName} stub {stubText}",
                    domainName,
                    stubText);
            }

            return idAndLink;
        }

        public async Task<Model.IdAndLink> GetStubNoGroupAsync(string stubText)
        {
            string cacheKey = GetCacheKey(stub: stubText);

            var cachedValue = await GetCacheAsync(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var idAndLink = await _context.Records
                .AsNoTracking()
                .Where(_ => _.IsActive
                    && _.GroupId == null
                    && _.Stub == stubText)
                .Select(_ => new Model.IdAndLink { Id = _.RecordId, Link = _.Link })
                .SingleOrDefaultAsync();

            if (idAndLink != null)
            {
                await SetCacheAsync(cacheKey, idAndLink);
            }
            else
            {
                _logger.LogInformation("No match for stub {stubText}",
                    stubText);
            }

            return idAndLink;
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
            else
            {
                _logger.LogInformation("No match for domain {domainName}",
                    domainName);
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
            else
            {
                _logger.LogWarning("No match for system default");
            }

            return idAndLink;
        }

        public string GetCacheKey(string domain = null, string stub = null)
        {
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(stub))
            {
                return $"d.{domain}.s.{stub}";
            }
            else if (!string.IsNullOrEmpty(domain))
            {
                return $"d.{domain}";
            }
            else if (!string.IsNullOrEmpty(stub))
            {
                return $"s.{stub}";
            }
            else
            {
                return "default";
            }
        }
    }
}
