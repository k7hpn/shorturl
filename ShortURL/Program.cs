using System;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using ShortURL;
using ShortURL.Model;
using ShortURL.Model.Keys;

const string ConnectionStringName = "DefaultConnection";
const string DefaultDatabaseProvider = "SqlServer";
const string DistributedCacheInMemory = "InMemory";
const string DistributedCacheRedis = "Redis";

const string EnvAspNetCoreEnv = "ASPNETCORE_ENVIRONMENT";
const string EnvRunningInContainer = "DOTNET_RUNNING_IN_CONTAINER";
const string OpenContainersImageCreated = "org.opencontainers.image.created";
const string OpenContainersImageRevision = "org.opencontainers.image.revision";
const string OpenContainersImageVersion = "org.opencontainers.image.version";

const string CannotParseReverseProxyIp = "Cannot parse reverse proxy IP address: {0}";
const string MissingCacheConfig = "Redis selected for distributed cache but not configured; set DistributedCacheConfiguration";
const string MissingConnectionString = "Missing connection string: {0}";
const string UnknownCacheType = "Unknown cache type requested: {0}";
const string UnknownDatabaseProvider = "Unknown database provider: {0}";

var applicationInfo = new ApplicationInformation
{
    { LogEnrichmentKeys.Application, Assembly.GetExecutingAssembly().GetName().Name
        ?? nameof(ShortURL) },
    { LogEnrichmentKeys.Startup, DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture)},
    { LogEnrichmentKeys.Version, Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Unknown"
    }
};

var builder = WebApplication.CreateBuilder(args);

var applicationConfiguration = new ApplicationConfiguration();

var configurationSection = applicationInfo[LogEnrichmentKeys.Application]
    .Replace(".", "", StringComparison.InvariantCultureIgnoreCase);

builder.Configuration.GetSection(configurationSection).Bind(applicationConfiguration);

builder.Services.AddSingleton(applicationConfiguration);

builder.Host.UseSerilog();

applicationInfo.Add(LogEnrichmentKeys.Environment,
    builder.Configuration[EnvAspNetCoreEnv] ?? "Production");

if (!string.IsNullOrEmpty(applicationConfiguration.Instance))
{
    applicationInfo.Add(LogEnrichmentKeys.Instance, applicationConfiguration.Instance ?? "Unknown");
}

if (!string.IsNullOrEmpty(builder.Configuration[EnvRunningInContainer]))
{
    if (!string.IsNullOrEmpty(builder.Configuration[OpenContainersImageCreated]))
    {
        applicationInfo.Add(LogEnrichmentKeys.ContainerCreated,
            builder.Configuration[OpenContainersImageCreated] ?? "Unknown");
    }
    if (!string.IsNullOrEmpty(builder.Configuration[OpenContainersImageRevision]))
    {
        applicationInfo.Add(LogEnrichmentKeys.ContainerRevision,
            builder.Configuration[OpenContainersImageRevision] ?? "Unknown");
    }
    if (!string.IsNullOrEmpty(builder.Configuration[OpenContainersImageVersion]))
    {
        applicationInfo.Add(LogEnrichmentKeys.ContainerImageVersion,
            builder.Configuration[OpenContainersImageVersion] ?? "Unknown");
    }
}

if (string.IsNullOrEmpty(applicationConfiguration.DistributedCache)
    || string.Equals(applicationConfiguration.DistributedCache,
        DistributedCacheInMemory,
        StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDistributedMemoryCache();
    applicationInfo.Add(LogEnrichmentKeys.DistributedCache, DistributedCacheInMemory);
}
else if (string.Equals(applicationConfiguration.DistributedCache,
    DistributedCacheRedis,
    StringComparison.OrdinalIgnoreCase))
{
    var cacheConfiguration = applicationConfiguration.DistributedCacheConfiguration
        ?? throw new ShortUrlException(MissingCacheConfig);

    var cacheDiscriminator = applicationConfiguration.DistributedCacheDiscriminator ?? string.Empty;

    builder.Services.AddStackExchangeRedisCache(_ =>
    {
        _.Configuration = cacheConfiguration;
        _.InstanceName = cacheDiscriminator;
    });

    applicationInfo.Add(LogEnrichmentKeys.DistributedCache, DistributedCacheRedis);
}
else
{
    throw new ShortUrlException(string.Format(CultureInfo.InvariantCulture,
        UnknownCacheType,
        applicationConfiguration.DistributedCache));
}

var defaultConnection = builder.Configuration.GetConnectionString(ConnectionStringName)
    ?? throw new ShortUrlException(string.Format(CultureInfo.InvariantCulture,
        MissingConnectionString,
        ConnectionStringName));

var databaseProvider = applicationConfiguration.DatabaseProvider ?? DefaultDatabaseProvider;

switch (databaseProvider.ToUpperInvariant())
{
    case "SQLSERVER":
        builder.Services
            .AddDbContextPool<ShortURL.Data.Context>(_ => _.UseSqlServer(defaultConnection));
        break;

    default:
        throw new ShortUrlException(string.Format(CultureInfo.InvariantCulture,
            UnknownDatabaseProvider,
            databaseProvider));
}

builder.Services.AddControllers();

builder.Services.AddScoped<ShortURL.Data.Lookup>();
builder.Services.AddScoped<ShortURL.Data.LogRequest>();

var app = builder.Build();

Log.Logger = LogConfiguration.Build(builder.Configuration, applicationInfo).CreateLogger();

try
{
    Log.Information("Starting up {Application} v{Version}",
        applicationInfo[LogEnrichmentKeys.Application],
        applicationInfo[LogEnrichmentKeys.Version]);

    if (!string.IsNullOrEmpty(applicationConfiguration.RequestLogging))
    {
        app.UseSerilogRequestLogging();
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
    }

    if (!string.IsNullOrEmpty(applicationConfiguration.ReverseProxy))
    {
        if (System.Net.IPAddress.TryParse(applicationConfiguration.ReverseProxy, out var ip))
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All,
                ForwardLimit = null,
                KnownProxies = { ip },
                RequireHeaderSymmetry = false,
            });
        }
        else
        {
            throw new ShortUrlException(string.Format(CultureInfo.InvariantCulture,
                CannotParseReverseProxyIp,
                applicationConfiguration.ReverseProxy));
        }
    }

    app.Use(async (context, next) =>
    {
        using (LogContext.PushProperty(LogEnrichmentKeys.Identifier,
                    context.TraceIdentifier))
        using (LogContext.PushProperty(LogEnrichmentKeys.RemoteAddress,
            context.Connection.RemoteIpAddress))
        {
            await next.Invoke();
        }
    });

    app.RunMigrations();

    app.UseRouting();
    app.UseEndpoints(_ => _.MapControllers());

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex,
        "Unexpected exception in {Application} v{Version}: {ErrorMessage}",
            applicationInfo[LogEnrichmentKeys.Application],
            applicationInfo[LogEnrichmentKeys.Version],
            ex.Message);
    throw;
}
finally
{
    if (long.TryParse(applicationInfo[LogEnrichmentKeys.Startup], out var startupTicks))
    {
        Log.Information("Shutting down {Application} v{Version} - uptime: {ApplicationUptime}",
            applicationInfo[LogEnrichmentKeys.Application],
            applicationInfo[LogEnrichmentKeys.Version],
            DateTime.Now - new DateTime(startupTicks));
    }
    else
    {
        Log.Information("Shutting down {Application} v{Version}",
            applicationInfo[LogEnrichmentKeys.Application],
            applicationInfo[LogEnrichmentKeys.Version]);
    }
    Log.CloseAndFlush();
}