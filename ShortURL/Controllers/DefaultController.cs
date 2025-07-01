using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ShortURL.Model;

namespace ShortURL.Controllers
{
    [Route("")]
    public class DefaultController : Controller
    {
        private const string MuteExtension = ".php";

        private static readonly string[] MuteStubs = [
            "favicon.ico",
            "index.htm",
            "robots.txt",
            "sitemap.xml",
            "webdav"
        ];

        private readonly IDistributedCache _cache;
        private readonly ApplicationConfiguration _config;
        private readonly ILogger _logger;
        private readonly Data.Lookup _lookup;
        private readonly Data.LogRequest _update;

        public DefaultController(ILogger<DefaultController> logger,
            IDistributedCache cache,
            ApplicationConfiguration config,
            Data.Lookup lookup,
            Data.LogRequest update)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(lookup);
            ArgumentNullException.ThrowIfNull(update);

            _cache = cache;
            _config = config;
            _logger = logger;
            _lookup = lookup;
            _update = update;
        }

        [HttpDelete("{stub}")]
        public async Task<IActionResult> Delete(string stub)
        {
            var domainNameText = Request?.Host.Host?.Trim();
            var stubText = stub?.Trim();

            var key = Data.Lookup.GetCacheKey(domainNameText, stubText);

            if (key != "default")
            {
                await _cache.RemoveAsync(key);
                _logger.LogInformation("Cache key {Key} purged upon request", key);
            }

            return Ok();
        }

        [Route("")]
        [HttpGet("{stub}")]
        public async Task<ActionResult<string>> Get(string stub)
        {
            return Redirect(await GetRedirectAsync(Request?.Host.Host, stub?.Trim()));
        }

        private async Task<string> GetRedirectAsync(string domainName,
            string stub)
        {
            string domainNameText = domainName?.Trim();
            string stubText = stub?.Trim();

            Model.IdAndLink recordIdLink = null;
            Model.IdAndLink groupIdLink = null;

            if (!string.IsNullOrEmpty(stubText))
            {
                if (!string.IsNullOrEmpty(domainNameText))
                {
                    // domain and stub provided, check group + stub
                    recordIdLink = await _lookup.GetGroupSlugAsync(domainNameText, stubText);
                }

                // no match for group + stub or stub provided with no domain
                // check for stub independent of domain/group
                recordIdLink ??= await _lookup.GetSlugNoGroupAsync(stubText);
            }

            if (recordIdLink == null && !string.IsNullOrEmpty(domainNameText))
            {
                // domain provided, no stub or stub not found; check group default
                groupIdLink = await _lookup.GetGroupDefaultAsync(domainNameText);
            }

            string destination;

            if (recordIdLink == null)
            {
                if (groupIdLink == null)
                {
                    groupIdLink = await _lookup.GetSystemDefault();
                    if (groupIdLink != null)
                    {
                        _logger.LogInformation("Group not found for domain {DomainNameText}, using default group: {GroupLink}",
                            domainNameText, groupIdLink?.Link);
                    }
                }

                if (groupIdLink == null)
                {
                    destination = _config.DefaultLink;
                    _logger.LogInformation("No default URL configured in the database, defaulting to {Destination} from configuration",
                        destination);
                }
                else
                {
                    if (!string.IsNullOrEmpty(stubText)
                        && !MuteStubs.Contains(stubText)
                        && !stubText.EndsWith(MuteExtension))
                    {
                        _logger.LogWarning("Unable to fulfill domain {DomainNameText}, stub {StubText}, sending to {Link}",
                            domainNameText,
                            stubText,
                            groupIdLink.Link);
                    }
                    destination = groupIdLink.Link;
                    await _update.UpdateGroupVisitAsync((int)groupIdLink.Id);
                }
            }
            else
            {
                destination = recordIdLink.Link;
                await _update.UpdateRecordVisitAsync((int)recordIdLink.Id);
            }

            return destination;
        }
    }
}