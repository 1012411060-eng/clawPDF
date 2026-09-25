# AutoSave settings compatibility tests

The tests compile the production settings sources and reference the **unchanged,
shipped `src/lib/DataStorage.dll`**. No replacement serializer or storage mock is
used. The test project is separate from the legacy application solution so it
can run without WPF, a printer installation, or the private signing certificate.

With the .NET 8 SDK, run from the repository root:

```powershell
# Windows: .NET Framework, including real isolated HKCU storage tests.
dotnet test tests/clawPDF.Settings.Tests/clawPDF.Settings.Tests.csproj -f net462

# Cross-platform: settings and actual INI file round trips.
dotnet test tests/clawPDF.Settings.Tests/clawPDF.Settings.Tests.csproj -f net8.0
```

Windows needs .NET Framework 4.6.2 or a later compatible 4.x runtime. Registry
tests create a random `HKCU\Software\clawPDF.Tests\<GUID>` subtree and remove it
in `finally`; they never touch a user's clawPDF configuration. The shipped
RegistryStorage also references the obsolete `Registry.DynData` member, so these
tests deliberately run on .NET Framework, not modern .NET.

With only the .NET 10 SDK/runtime installed, the portable suite can also run as:

```sh
dotnet test tests/clawPDF.Settings.Tests/clawPDF.Settings.Tests.csproj -p:TargetFrameworks=net10.0 -f net10.0
```

The portable run explicitly skips actual registry access and Windows path
composition. It does not constitute a Windows print-job test.

Coverage: native and serialized drive/UNC paths; escape-looking directory names;
drive/share roots and trailing separators; Unicode and tokens; empty/missing
values; legacy relative paths; unchanged escapes in other settings; full profile
loading; unchanged on-disk serialization; INI and registry load/save/reload;
reproduction of the original Regex.Unescape failure. The Windows CI workflow also
compiles the original `clawPDF.Settings.csproj`.
