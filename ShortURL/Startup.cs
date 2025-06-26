using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ShortURL
{
    public class Startup
    {
        private readonly IConfiguration _config;

        private bool _isRelational;
        private bool _isDevelopment;

        public Startup(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _isDevelopment = env.IsDevelopment();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (_isDevelopment)
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

            app.UseRouting();
            app.UseEndpoints(_ => _.MapControllers());
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression(_ => _.Providers
                .Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>());

            services.AddHttpContextAccessor();

            string instance = _config[Program.ConfigurationInstance] ?? Program.DefaultInstance;

            if (!string.IsNullOrEmpty(_config[Program.ConfigurationRedis]))
            {
                string redisNamespace = _config[Program.ConfigurationRedisNamespace] ?? instance;

                services.AddStackExchangeRedisCache(_ =>
                {
                    _.Configuration = _config[Program.ConfigurationRedis];
                    _.InstanceName = redisNamespace + ".";
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
                _isRelational = true;
            }
            else
            {
                throw new Exception("No connection string provided.");
            }

            services.AddScoped<Data.Lookup>();
            services.AddScoped<Data.Update>();

            services.AddControllers();
        }

        private void ConfigureDatabase(IApplicationBuilder app)
        {
            if (_isRelational)
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
                            //_logger.LogInformation("There are {Count} pending database migrations",
                            //    pending.Count());

                            try
                            {
                                context.Migrate();
                            }
                            catch (Exception ex)
                            {
                                //_logger.LogCritical(ex,
                                //    "Could not perform database migration: {Message}",
                                //    ex.Message);
                                throw;
                            }
                        }
                    }
                }
            }
            else
            {
                //_logger.LogInformation("Not performing migrations, data store is non-relational.");
            }
        }
    }
}