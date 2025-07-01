# ShortURL

ShortURL is a cross-platform Web-based application for creating URLs and tracking click counts. 
Written using [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) and designed
to run in [Docker](https://www.docker.com/).

**NOTE: Missing from the v1.0.0 beta releases is any ability to add, edit, or remove URLs. 
In order to use the application you must manage the SQL Server database manually.**

## Configuring logging with Seq

If you wish to log to Seq, ensure your `appsettings.json` contians the following:

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
Serilog__WriteTo__1__Name=Seq
Serilog__WriteTo__1__Args__apiKey=apiKey
Serilog__WriteTo__1__Args__controlLevelSwitch=$controlSwitch
Serilog__WriteTo__1__Args__serverUrl=http://seq:5341
```

Note that the `Microsoft` and `System` namespaces are overridden in the top level
`appsettings.json` by default to only display "warning" and above, if you want them to use the 
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
