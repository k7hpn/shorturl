using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using ShortURL.Model;
using ShortURL.Model.Keys;

namespace ShortURL
{
    internal static class LogConfiguration
    {
        internal static LoggerConfiguration Build(IConfiguration config,
            ApplicationConfiguration applicationConfiguration,
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

            loggerConfig.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(applicationConfiguration.SeqEndpoint))
            {
                var levelSwitch = new LoggingLevelSwitch();

                loggerConfig
                    .WriteTo.Logger(_ => _
                        .WriteTo.Seq(applicationConfiguration.SeqEndpoint,
                            apiKey: applicationConfiguration.SeqApiKey,
                            controlLevelSwitch: levelSwitch));
            }

            return loggerConfig;
        }
    }
}