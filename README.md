# PJLink Projector control plugin for Essentials

Created using Essentials Plugin Template (c) 2020

## License

Provided under MIT license

## Overview

There are 2 versions:

1. "epi-PjLink-projector.sln" is a VS2008 solution for PepperDash Essentials v1.xx, the classes are in the /src folder.
2. "./epi-pjlink-v2.0/epi-pjlink-v2.0.sln" is a VS2022 solution for PepperDash Essentials v2.xx.

## Configuration

```json
{
    "key": "display-1",
    "name": "display",
    "type": "pjlink",
    "group": "displays",
    "properties": {
        "control": {
            "method": "Tcpip",
            "tcpSshProperties": {
                "autoReconnect": true,
                "AutoReconnectIntervalMs": 5000,
                "address": "192.168.0.60",
                "port": 4352,
                "username": "",
                "password": "JBMIAProjectorLink"
            }
        }
    }
}
```

### Functions tested

* Power control
  
### Functions not tested

* Password requirement.
* Simpl bridge.
* Source selection.
* Image mute
* Image freeze
* Audio mute
* Volume (PJLink does not support volume feedback, only up and down)

### Functions not implemented

* Password is currently hard coded to the default "JBMIAProjectorLink"

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
Alternatively, you can simply run the `GetPackages.bat` file.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.