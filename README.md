# ShortURL

ShortURL is a cross-platform Web-based application for creating URLs and tracking click counts.
Written using [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) and designed
to run in [Docker](https://www.docker.com/).

**_NOTE: Missing from the v1.0.0 beta releases is any ability to add, edit, or remove URLs.
In order to use the application you must manage the SQL Server database manually._**

## Application settings

The following application settings can be configured in the environment or the `appsettings.json`
file. These are belong in the "ShortURL" object in the configuration or prefixed with
`ShortURL__` in a Docker environment file.

- DatabaseProvider (required) - database provider to use, `SQLServer` for Microsoft SQL Server is supported
- DefaultLink (required) - default URL to redirect to if no additional metadata is provided/configured
- DistributedCache (optional) - cache system to use, options are `InMemory` and `Redis`, defaults to `InMemory` if not specified
- DistributedCacheConfiguration (optional) - cache configuration if `Redis` is selected, required when using Redis
- DistributedCacheDiscriminator (optional) - text providing a discriminator for the distributed cache provider, should be the same for all instances of the application using the same distributed cache
- Instance (optional) - name of the instance, should be unique for each running copy accessing same database, defaults to "Unknown"
- RequestLogging (optional) - whether or not to activate `UseSerilogRequestLogging()` and log all Web accesses, deactivated by default, activated with any content
- ReverseProxy (optional) - the IP address of a reverse proxy in front of the site

_NOTE: settings regarding Redis use the [AddStackExchangeRedisCache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed) module which supports Redis-compatible
value/key stores like [Valkey](https://valkey.io/)._

## Configuring logging with Seq

If you wish to log to Seq, ensure your `appsettings.json` contians the following (exclude `apiKey`
if you are not going to configure one):

```json
"Serilog": {
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "Seq",
      "Args": {
        "apiKey": "apiKey",
        "serverUrl": "http://seq:5341"
      }
    }
  ]
}
```

If you desire to use the dynamic level control, you can include that with settings as follows:

```json
"Serilog": {
  "LevelSwitches": { "$controlSwitch": "Verbose" },
  "MinimumLevel": {
    "ControlledBy": "$controlSwitch"
  },
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "Seq",
      "Args": {
        "apiKey": "apiKey",
        "controlLevelSwitch": "$controlSwitch",
        "serverUrl": "http://seq:5341"
      }
    }
  ]
}
```

If you are using Docker to deploy the application, use a configuration like this in the
environment file:

```
Serilog__LevelSwitches__$controlSwitch=Verbose
Serilog__MinimumLevel__ControlledBy=$controlSwitch
Serilog__WriteTo__0__Name=Console
Serilog__WriteTo__1__Args__apiKey=apiKey
Serilog__WriteTo__1__Args__controlLevelSwitch=$controlSwitch
Serilog__WriteTo__1__Args__serverUrl=http://seq:5341
Serilog__WriteTo__1__Name=Seq
```

Note that the `Microsoft` and `System` namespaces are overridden in the top level
`appsettings.json` by default to only display "Warning" and above, if you want them to use the
same level as the level switch, ensure this is placed in the `"MinimumLevel"` object:

```json
    "Override": {
      "Microsoft": "$controlSwitch",
      "System": "$controlSwitch"
    }
```

For a Docker environment file use:

```
Serilog__MinimumLevel__Override__Microsoft=$controlSwitch
Serilog__MinimumLevel__Override__System=$controlSwitch
```

## License

ShortURL source code is Copyright 2019 by the
[Maricopa County Library District](https://mcldaz.org/) and is distributed under
[The MIT License](http://opensource.org/licenses/MIT/).
