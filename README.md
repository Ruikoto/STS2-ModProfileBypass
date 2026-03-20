# ModProfileBypass

A [Slay the Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) mod that redirects the modded save profile directory back to the vanilla one, so you can keep using your original saves while mods are loaded.

## What it does

- Strips the `modded/` prefix from the profile directory path, making the game read/write saves from the same location as unmodded play.
- Hides itself from both local and server-side mod lists, so it won't affect multiplayer compatibility.

## Installation

1. Download `ModProfileBypass.dll` and `ModProfileBypass.json` from [Releases](https://github.com/Ruikoto/STS2-ModProfileBypass/releases).
2. Place them into your STS2 mods folder (e.g. `Slay the Spire 2/mods/ModProfileBypass/`).
3. Launch the game — your vanilla saves should now load normally.

## Building from source

Requires .NET 9.0 SDK. Game DLL references (`sts2.dll`, `GodotSharp.dll`, `0Harmony.dll`) are expected two directories up from the project root.

```bash
dotnet build
```

## License

[MIT](LICENSE)
