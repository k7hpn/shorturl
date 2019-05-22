using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ShortURL.Data
{
    public class Lookup
    {
        private readonly IDistributedCache _cache;
        private readonly Context _context;

        public Lookup(IDistributedCache cache, Context context)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private async Task<Model.IdAndLink> GetCacheAsync(string key)
        {
            var cacheValue = await _cache.GetAsync(key);
            if (cacheValue == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream(cacheValue))
            {
                return new BinaryFormatter().Deserialize(memoryStream) as Model.IdAndLink;
            }
        }

        private async Task SetCacheAsync(string key, Model.IdAndLink idAndLink)
        {
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
