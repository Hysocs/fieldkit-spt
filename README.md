# FieldKit

FieldKit is an in-raid administration and testing toolkit for SPT users, server
administrators, and mod developers.

## Repository layout

- `Client/` — BepInEx client plugin and client project
- `Server/` — SPT server mod and server project
- `dist/` — generated release packages

## Features

- Character controls, including movement and player-state tools
- Weapon tuning, ammo controls, and weapon diagnostics
- Entity browsing and actions for players and loot
- Searchable loot catalog with item spawning tools
- ESP, labels, chams, and other world visualization options
- Additional testing utilities for doors, extracts, and raid entities

## Installation

Extract the release ZIP into your SPT installation directory, then restart the
SPT server and game. The archive installs:

```text
BepInEx/plugins/Hysocs-FieldKit/FieldKit.dll
SPT/user/mods/HysocsFieldKit/FieldKit.Server.dll
```

To uninstall FieldKit, remove the `Hysocs-FieldKit` and `HysocsFieldKit`
folders shown above.

## Usage

- Press `Insert` while in a raid to open or close the FieldKit menu.
- Press `Home` to toggle ESP.
- Press `F12` to open the BepInEx configuration menu, where FieldKit settings
  and hotkeys can also be changed.

## Building

Open `FieldKit.sln` and build the entire solution in the `Release`
configuration, or run:

```powershell
dotnet build FieldKit.sln -c Release -p:SkipDeploy=true
```

The Release solution build creates `dist/FieldKit-1.4.0.zip` with both DLLs in
the ready-to-install SPT 4.1 directory structure: the client is placed under
`BepInEx/plugins`, and the server mod under `SPT_Runtime/user/mods`.
`SkipDeploy=true` prevents the build from copying files into the active SPT
installation; use `SkipPackage=true` if a ZIP is not wanted.

## License

FieldKit is licensed under the [Apache License 2.0](LICENSE).
