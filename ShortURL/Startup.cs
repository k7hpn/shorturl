using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ShortURL
{
    public class Startup
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private bool IsRelational = false;

        public Startup(IConfiguration config,
            ILogger<Startup> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression(_ => _.Providers
                .Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>());

            string instance = _config[Program.ConfigurationInstance] ?? Program.DefaultInstance;

            if (!string.IsNullOrEmpty(_config[Program.ConfigurationRedis]))
            {
                services.AddDistributedRedisCache(_ =>
                {
                    _.Configuration = _config[Program.ConfigurationRedis];
                    _.InstanceName = instance + ".";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            var cs = _config.GetConnectionString(Program.ConnectionString);
            if (!string.IsNullOrEmpty(cs))
            {
                services.AddDbContextPool<Data.Context>(_ => _.UseSqlServer(cs));
                IsRelational = true;
            }
            else
            {
                services.AddDbContextPool<Data.Context>(_ => _.UseInMemoryDatabase(instance));
                IsRelational = false;
            }

            services.AddScoped<Data.Lookup>();
            services.AddScoped<Data.Update>();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (!string.IsNullOrEmpty(_config[Program.ConfigurationReverseProxy]))
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All,
                    RequireHeaderSymmetry = false,
                    ForwardLimit = null,
                    KnownProxies = {
                        System.Net.IPAddress.Parse(_config[Program.ConfigurationReverseProxy])
                    }
                });
            }

            app.Use(async (context, next) =>
            {
                using (LogContext.PushProperty(LogConfig.IdentifierEnrichment,
                    context.TraceIdentifier))
                using (LogContext.PushProperty(LogConfig.RemoteAddressEnrichment,
                    context.Connection.RemoteIpAddress))
                {
                    await next.Invoke();
                }
            });

            ConfigureDatabase(app);

            app.UseMvc();
        }

        private void ConfigureDatabase(IApplicationBuilder app)
        {
            if (IsRelational)
            {
                using (var scope = app
                    .ApplicationServices
                    .GetRequiredService<IServiceScopeFactory>()
                    .CreateScope())
                {
                    using (var context = scope.ServiceProvider.GetRequiredService<Data.Context>())
                    {
                        var pending = context.GetPendingMigrations();

                        if (pending?.Count() > 0)
                        {
                            _logger.LogInformation("There are {Count} pending database migrations",
                                pending.Count());

                            try
                            {
                                context.Migrate();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogCritical(ex,
                                    "Could not perform database migration: {Message}",
                                    ex.Message);
                                throw;
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("Not performing migrations, data store is non-relational.");
            }
        }
    }
}
