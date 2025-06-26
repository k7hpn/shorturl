using System;
using System.Data;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ShortURL
{
    public class LogConfig
    {
        private const string ApplicationEnrichment = "Application";
        private const string VersionEnrichment = "Version";
        private const string InstanceEnrichment = "Instance";

        public const string IdentifierEnrichment = "Identifier";
        public const string RemoteAddressEnrichment = "RemoteAddress";

        public LoggerConfiguration Build(IConfiguration config, LoggingLevelSwitch levelSwitch)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            LoggerConfiguration loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .Enrich.WithProperty(ApplicationEnrichment,
                    Assembly.GetExecutingAssembly().GetName().Name)
                .Enrich.WithProperty(VersionEnrichment,
                    Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);

            string instance = config[Program.ConfigurationInstance];

            if (!string.IsNullOrEmpty(instance))
            {
                loggerConfig.Enrich.WithProperty(InstanceEnrichment, instance);
            }

            loggerConfig.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

            return loggerConfig;
        }
    }
}
