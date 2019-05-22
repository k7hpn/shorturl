using System.Data;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace ShortURL
{
    public class LogConfig
    {
        private const string ApplicationEnrichment = "Application";
        private const string VersionEnrichment = "Version";
        private const string InstanceEnrichment = "Instance";

        public const string IdentifierEnrichment = "Identifier";
        public const string RemoteAddressEnrichment = "RemoteAddress";

        public LoggerConfiguration Build(IConfiguration config, string instance)
        {
            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty(ApplicationEnrichment,
                    Assembly.GetExecutingAssembly().GetName().Name)
                .Enrich.WithProperty(VersionEnrichment,
                    Assembly.GetExecutingAssembly().GetName().Version)
                .Enrich.WithProperty(InstanceEnrichment, instance)
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(config)
                .WriteTo.Console();

            string rollingLog = config[Program.ConfigurationLogRolling];
            if (!string.IsNullOrEmpty(rollingLog))
            {
                string rollingLogFile = rollingLog + instance + "-.log";

                loggerConfig.WriteTo.File(rollingLogFile, rollingInterval: RollingInterval.Day);
            }

            string sqlLog = config.GetConnectionString(Program.LogConnectionString);
            if (!string.IsNullOrEmpty(sqlLog))
            {
                loggerConfig
                    .WriteTo.Logger(_ => _
                    .WriteTo.MSSqlServer(sqlLog,
                        "Logs",
                        autoCreateSqlTable: true,
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        columnOptions: new ColumnOptions
                        {
                            AdditionalDataColumns = new DataColumn[]
                            {
                                new DataColumn(ApplicationEnrichment, typeof(string)) { MaxLength = 255 },
                                new DataColumn(VersionEnrichment, typeof(string)) { MaxLength = 255 },
                                new DataColumn(IdentifierEnrichment, typeof(string)) { MaxLength = 255 },
                                new DataColumn(InstanceEnrichment, typeof(string)) { MaxLength = 255 },
                                new DataColumn(RemoteAddressEnrichment, typeof(string)) { MaxLength = 255 }
                            }
                        }));
            }

            return loggerConfig;
        }
    }
}
