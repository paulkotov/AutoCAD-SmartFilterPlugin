# AcSmartFilterPlugin

AutoCAD plugin for smart filtering and selecting drawing objects by multiple criteria. Instead of manually picking entities one by one, you define filter rules and the plugin finds matching objects in the current drawing.

## Features

The filter dialog lets you combine criteria to narrow down the selection:

- **Layer** — filter objects on a specific layer or set of layers
- **Color** — match by entity color (ByLayer, ByBlock, or explicit color index / true color)
- **Object type** — limit results to lines, polylines, blocks, text, dimensions, and other entity types
- **Additional criteria** — extendable filter set for properties such as linetype, lineweight, block name, and more

Matching objects can be selected in the drawing for further editing, inspection, or batch operations.

## Requirements

- **AutoCAD** with .NET API support (ObjectARX managed assemblies)
- **.NET Framework 4.8**
- **x64** platform (AutoCAD is 64-bit)
- **Visual Studio** (2019 or later recommended) for building from source

AutoCAD managed references (`AcCoreMgd`, `AcDbMgd`, `AcMgd`) must be available on the build machine — typically resolved from the AutoCAD installation via Visual Studio project settings.

## Installation

1. Build the solution in **Release** configuration.
2. In AutoCAD, run `NETLOAD`.
3. Browse to `AcSmartFilterPlugin\bin\Release\AcSmartFilterPlugin.dll` and load the assembly.

The plugin loads once per AutoCAD session.

## Usage

1. Open a drawing in AutoCAD.
2. Run the smart filter command (command name to be defined in `myCommands.cs`).
3. In the filter window, set the desired criteria — layer, color, object type, and any other available filters.
4. Apply the filter to select matching objects in the drawing.

## Project structure

```
AcSmartFilterPlugin/
├── AcSmartFilterPlugin.sln          # Solution file
├── AcSmartFilterPlugin/
│   ├── AcSmartFilterPlugin.cs       # Plugin entry point (IExtensionApplication)
│   ├── Commands/
│   │   └── SmartFilterCommands.cs   # AutoCAD commands
│   └── Views/
│       ├── FilterView.xaml          # Filter dialog UI (FilterView)
│       └── FilterView.xaml.cs
└── README.md
```

## Development

### Build

```powershell
msbuild AcSmartFilterPlugin.sln /p:Configuration=Release
```

Or open `AcSmartFilterPlugin.sln` in Visual Studio and build with **Release | Any CPU**.

### Tech stack

- C# / .NET Framework 4.8
- AutoCAD .NET API
- WPF for the filter dialog

## License

Copyright © HP 2026
