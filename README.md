# AcSmartFilterPlugin

AutoCAD plugin that temporarily filters the drawing by **layer visibility**. You choose one or more layers (or the current layer); the plugin turns those layers on and turns all other layers off. Reset or closing the panel restores the previous on/off and freeze state.

## Features

- **Layer list** — multi-select layers from the active drawing
- **Current layer mode** — filter by the drawing’s current layer only
- **Apply / Reset** — apply the visibility filter, or restore the original layer state
- **Ribbon entry** — **Smart Filter** tab → **Filters** panel → **Smart Filter** button
- **DWG persistence** — filter configuration is saved inside the DWG file using the AutoCAD **XDataRecord** API, so settings travel with the drawing

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

On load, the plugin creates the **Smart Filter** ribbon tab (or waits until the ribbon is ready). The plugin loads once per AutoCAD session.

## Usage

1. Open a drawing in AutoCAD.
2. Run `SmartFilter`, or click **Smart Filter** on the ribbon.
3. In the **Выбор данных** window:
   - Select one or more layers in the list, **or**
   - Check **Фильтровать по текущему слою**.
4. Click **Применить** to isolate the selected layers (others are turned off).
5. Click **Сбросить** to restore the original layer state, or close the window to restore automatically.

## Commands

| Command | Description |
|---------|-------------|
| `SmartFilter` | Opens (or activates) the modeless filter panel |

## Project structure

```
AcSmartFilterPlugin/
├── AcSmartFilterPlugin.sln
├── AcSmartFilterPlugin/
│   ├── AcSmartFilterPlugin.cs       # IExtensionApplication + ribbon
│   ├── Commands/
│   │   └── SmartFilterCommands.cs   # SmartFilter command
│   ├── Services/
│   │   ├── ILayerFilterService.cs
│   │   └── LayerFilterService.cs    # Layer load / apply / restore
│   ├── Models/
│   │   ├── LayerInfo.cs
│   │   └── LayerSnapshot.cs
│   ├── ViewModels/
│   │   ├── FilterViewModel.cs
│   │   └── LayerListItem.cs
│   ├── Views/
│   │   ├── FilterView.xaml
│   │   └── FilterView.xaml.cs
│   └── Mvvm/
│       └── RelayCommand.cs
└── README.md
```

## Development

### Build

```powershell
msbuild AcSmartFilterPlugin.sln /p:Configuration=Release
```

Or open `AcSmartFilterPlugin.sln` in Visual Studio and build with **Release | Any CPU** (the project targets **x64**).

### Tech stack

- C# / .NET Framework 4.8
- AutoCAD .NET API
- WPF + MVVM for the filter panel

## License

Copyright © HP 2026
