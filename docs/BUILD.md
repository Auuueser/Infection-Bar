# Build

Requires .NET 8 SDK, Lethal Company V81, and a BepInEx 5 profile with LethalConfig installed.

Set `INFECTIONBAR_GAME_DIR` to the game installation directory and `INFECTIONBAR_PROFILE_DIR` to the profile directory, then run:

```powershell
dotnet build IndependentCadaverInfectionBar.csproj -c Release
```

Alternatively, pass the paths using `-p:GameDir="..." -p:TestProfileDir="..."`.

Output: `bin/Release/netstandard2.1/InfectionBar.dll`. Game assemblies are referenced locally and are not distributed.
