using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ShortURL
{
    public static class Program
    {
        public const string ConfigurationDefaultLink = "ShortURL.DefaultLink";
        public const string ConfigurationInstance = "ShortURL.Instance";
        public const string ConfigurationLogRolling = "ShortURL.LogRolling";
        public const string ConfigurationRedis = "ShortURL.RedisConfiguration";
        public const string ConfigurationReverseProxy = "ShortURL.ReverseProxy";

        public const string ConnectionString = "ShortURL";
        public const string LogConnectionString = "ShortURL.LogSql";

        public const string DefaultInstance = "ShortURL";

        public static void Main(string[] args)
        {
            string instance = DefaultInstance;

            using (var configWebhost = CreateWebHostBuilder(args).Build())
            using (var scope = configWebhost.Services.CreateScope())
            {
                var startupConfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                if (!string.IsNullOrEmpty(startupConfig[ConfigurationInstance]))
                {
                    instance = startupConfig[ConfigurationInstance];
                }

                Log.Logger = new LogConfig().Build(startupConfig, instance).CreateLogger();
            }

            try
            {
                Log.Information("Starting Web host for instance {Instance}", instance);
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (System.Exception ex)
            {
                Log.Logger.Fatal(ex, "A fatal error occurred on instance {Instance}: {Message}",
                    instance,
                    ex.Message);
            }
            finally
            {
                Log.Information("Stopping Web host for instance {Instance}", instance);
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseStartup<Startup>();
    }
}
