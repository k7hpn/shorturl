using System;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ShortURL
{
    public static class Program
    {
        private const string EnvAspNetCoreEnv = "ASPNETCORE_ENVIRONMENT";
        private const string EnvRunningInContainer = "DOTNET_RUNNING_IN_CONTAINER";

        public const string ConfigurationDefaultLink = "ShortURL.DefaultLink";
        public const string ConfigurationInstance = "ShortURL.Instance";
        public const string ConfigurationLogRolling = "ShortURL.LogRolling";
        public const string ConfigurationRedis = "ShortURL.RedisConfiguration";
        public const string ConfigurationRedisNamespace = "ShortURL.RedisNamespace";
        public const string ConfigurationReverseProxy = "ShortURL.ReverseProxy";

        public const string ConnectionString = "ShortURL";
        public const string LogConnectionString = "ShortURL.LogSql";

        public const string DefaultInstance = "ShortURL";

        public static int Main(string[] args)
        {
            using var webhost = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(_ => _.UseStartup<Startup>())
                .UseSerilog()
                .Build();
            
            using var scope = webhost.Services.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var instance = config[ConfigurationInstance] ?? DefaultInstance;

            var loggingLevelSwitch = new Serilog.Core.LoggingLevelSwitch();

            Log.Logger = new LogConfig().Build(config, loggingLevelSwitch).CreateLogger();
            Log.Information("Starting {Name} v{Version} instance {Instance} environment {Environment}",
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()?
                    .Version,
                instance,
                config[EnvAspNetCoreEnv] ?? "Production");

            Log.Information("Starting {Name} v{Version} for instance {Instance}",
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()?
                    .Version,
                instance);

            if (!string.IsNullOrEmpty(config[EnvRunningInContainer]))
            {
                Log.Information("Containerized: commit {ContainerCommit} created on {ContainerDate} image {ContainerImageVersion}",
                    config["org.opencontainers.image.revision"] ?? "unknown",
                    config["org.opencontainers.image.created"] ?? "unknown",
                    config["org.opencontainers.image.version"] ?? "unknown");
            }

            try
            {
                webhost.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex,
                    "{Name} v{Version} instance {Instance} exited unexpectedly: {Message}",
                    Assembly.GetExecutingAssembly().GetName().Name,
                    Assembly.GetEntryAssembly()
                        .GetCustomAttribute<AssemblyFileVersionAttribute>()?
                        .Version,
                    instance,
                    ex.Message);
                Environment.ExitCode = 1;
                throw;
            }
            finally
            {
                Log.Information("Stopping {Name} v{Version} instance {Instance}",
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()?
                    .Version,
                instance);
                Log.CloseAndFlush();
            }
        }
    }
}
