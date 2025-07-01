using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using ShortURL.Model.Keys;

namespace ShortURL
{
    internal static class LogConfiguration
    {
        internal static LoggerConfiguration Build(IConfiguration config,
            IDictionary<string, string> applicationInfo)
        {
            ArgumentNullException.ThrowIfNull(config);

            LoggerConfiguration loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext();

            foreach (var key in applicationInfo.Keys)
            {
                if (key == LogEnrichmentKeys.Startup
                    && long.TryParse(applicationInfo[key],
                        out var ticks))
                {
                    loggerConfig.Enrich.WithProperty(key, new DateTime(ticks).ToString("O"));
                }
                else
                {
                    loggerConfig.Enrich.WithProperty(key, applicationInfo[key]);
                }
            }

            return loggerConfig;
        }
    }
}