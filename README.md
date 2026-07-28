# FieldKit

FieldKit is an in-raid administration and testing toolkit for SPT users, server
administrators, and mod developers.

## Features

- Character controls, including movement and player-state tools
- Weapon tuning, ammo controls, and weapon diagnostics
- Entity browsing and actions for players and loot
- Searchable loot catalog with item spawning tools
- ESP, labels, chams, and other world visualization options
- Additional testing utilities for doors, extracts, and raid entities

## Installation

Extract the release ZIP into your SPT installation directory, then restart the
SPT server and game.

## Building

Open `FieldKit.sln` or build `FieldKit.csproj`.

A Release build creates `dist/FieldKit-1.7.0.zip` with the ready-to-install SPT
directory structure. Use `-p:SkipDeploy=true` to build without installing or
packaging the mod.

## License

FieldKit is licensed under the [Apache License 2.0](LICENSE).
