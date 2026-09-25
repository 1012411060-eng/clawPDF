# AutoSave.TargetDirectory compatibility

## Scope and cause

Base: clawPDF 0.9.3, commit `0785746b5929dcb16856995a04438ec753830dbc`.
The fix only changes `src/clawPDF.Settings/AutoSave.cs` in production.

The real settings data flow is:

1. `SettingsHelper.CreateEmptySettings()` selects `RegistryStorage` with
   `RegistryHive.CurrentUser` and `Software\clawSoft\clawPDF\Settings`.
2. `SettingsHelper.LoadSettings()` / `ReloadSettings()` calls
   `clawPDFSettings.LoadData()`. INI import uses the same settings loader with
   `IniStorage` instead.
3. The storage reads literal string values into `Data`. Neither storage performs
   backslash decoding (`IniStorage` does trim whitespace around values).
4. `clawPDFSettings.ReadValues()` loads `ConversionProfiles\0\`, then
   `ConversionProfile.ReadValues()` loads `AutoSave\`.
5. Previously, `AutoSave.ReadValues()` always passed TargetDirectory to
   `Data.UnescapeString()`. Its catch-all handler replaced any failure with `""`.
6. `AutoSaveWorkflow.QueryTargetFile()` combines the loaded directory with the
   document filename. `ProfileChecker.CheckAutosaveSettings()` rejects an empty
   target with `Automatic saving without target directory.`

The implementation of the shipped `src/lib/DataStorage.dll` was inspected by
decompilation and exercised directly in the tests:

- `Data.EscapeString()` encodes backslash, CR, LF and NUL as `\\`, `\r`, `\n`
  and `\0`. `AutoSave.StoreValues()` calls it before writing to either storage.
- `Data.UnescapeString()` calls `System.Text.RegularExpressions.Regex.Unescape()`.
- A native `C:\Temp` throws because `\T` is not a recognized regex escape. The
  native PowerHaus UNC example likewise throws on an unrecognized escape.
- `C:\new\test` is worse: `\n` and `\t` decode successfully into control
  characters. Merely falling back to the raw value **after an exception** would
  not fix this case.

## Decoding rule and compatibility

Only TargetDirectory now recognizes native absolute Windows path syntax *before*
calling the legacy decoder:

- A drive letter followed by `:\` with a single root separator: preserve literally.
- A UNC prefix with exactly two leading backslashes and a single server/share
  separator: preserve literally.
- Otherwise use `Data.UnescapeString()` as before; missing/empty stays empty.

Actual string contents (not C# or `.reg` file literals):

| Stored value | Internal value |
| --- | --- |
| `C:\Temp` | `C:\Temp` |
| `C:\\Temp` | `C:\Temp` |
| `D:\PowerHaus\Dokumente` | `D:\PowerHaus\Dokumente` |
| `\\pwrhaus\temp\dir` | `\\pwrhaus\temp\dir` |
| `\\\\pwrhaus\\temp\\dir` | `\\pwrhaus\temp\dir` |

Existing serialized absolute Windows paths have doubled separators, so they
continue through the original decoder. Requiring a single server/share separator
also avoids mistaking serialized root-relative paths (`\\reports\\daily`) for
native UNC paths. Existing relative and token-based serialized paths are covered
by regression tests.

This does not add normalization, repair malformed/mixed encodings, or change the
Windows path-length rules. It does not promise support for externally written
unescaped relative paths. Ambiguous strings that resemble a native absolute path
are interpreted as paths, not as deliberately encoded control characters; control
characters are not valid Windows directory-name characters. Other setting fields
retain their original escape semantics. Saving always uses the existing
`Data.EscapeString()`, including after importing a native path.

Registry location, profile selection, GUIDs, AutoSave flags, and the workflow are
unchanged. In RDS, settings remain in the current user's HKCU. The fix does not
introduce machine-wide state or write the registry while reading settings.

## Generated source maintenance

`AutoSave.cs` carries a generated-file warning. The helper is inside its existing
`START_CUSTOM_SECTION:GENERAL` block. The one-line call in `ReadValues()` also
has to be preserved if settings classes are regenerated. This release includes
`settings.xml` but no settings generator/template to update. The regression tests
catch losing that call; a maintainer with the generator should carry the same
field-specific hook into its template. There is no global DataStorage change.

## Upstream investigation

Checked on 2026-09-25: the official `master` and release tag `0.9.3` both resolve
to the commit above. GitHub issue/PR searches for `TargetDirectory`, `backslash`,
`escape` and `PowerHaus` found no exact report. All available PR titles were also
reviewed; no corresponding fix was found. This is a search result, not a claim
that every discussion or comment has been inspected.

Related but not confirmed duplicates:

- [#57: registry settings rewritten/deleted](https://github.com/clawsoftware/clawPDF/issues/57).
  The maintainer points to `/Config` for configuration deployment.
- [#171: profiles reset to defaults](https://github.com/clawsoftware/clawPDF/issues/171).
- [#175: AutoSave target not used](https://github.com/clawsoftware/clawPDF/issues/175).
  Its report does not establish a backslash-serialization cause.

Existing `/Config` imports use the same decoder. `/PrintFile=... /OutputPath=...`
is a different batch-printing entry point (`AppStartFactory` and
`AutoSaveWorkflow`); it does not change how PowerHaus's existing TargetDirectory
registry writes are decoded. No native-path compatibility switch was found.

## Tests and Windows build

See [test instructions](../tests/clawPDF.Settings.Tests/README.md). The Windows CI
workflow runs the actual .NET Framework registry tests and builds the production
settings project. A local macOS run covers the real INI reader/writer and the
real serializer, but cannot validate Windows registry access or printing.
A local Windows 11 ARM64 run targeting .NET Framework 4.6.2 passed all 102 tests
(0 skipped, 0 failed), including HKCU access. The full managed WPF application,
SetupHelper and Bridge also built successfully with Visual Studio 2022. Native
and serialized local/UNC inputs were additionally checked against the compiled
production Settings.dll. After receiving the private Windows installer, the
requester confirmed that it works correctly in their test setup. The precise
Windows/share configuration and the full multi-user acceptance matrix were not
independently recorded; the acceptance test below remains useful for broader
RDS/SMB validation.

The upstream [build guide](https://github.com/clawsoftware/clawPDF/wiki/Build-it-yourself)
specifies Visual Studio 2022 with C# and C++ plus the Visual Studio Installer
Projects 2022 extension. For the managed application, install the .NET desktop
workload and **.NET Framework 4.6.2 targeting/developer pack**. Use full Visual
Studio MSBuild on Windows for WPF; `dotnet build` on macOS cannot build the full
application. Install the .NET 8 SDK for the separate test project.

From the repository root in a Visual Studio Developer PowerShell:

```powershell
# Execute regression tests, including real HKCU access.
dotnet test .\tests\clawPDF.Settings.Tests\clawPDF.Settings.Tests.csproj -f net462

# Build just the modified production assembly.
msbuild .\src\clawPDF.Settings\clawPDF.Settings.csproj /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /p:SignAssembly=false

# Build the managed application and its project dependencies.
$repo = (Get-Location).Path + '\'
msbuild .\src\clawPDF\clawPDF.csproj /restore /t:Rebuild /p:RestorePackagesConfig=true /p:Configuration=Release /p:Platform=AnyCPU /p:SignAssembly=false "/p:SolutionDir=$repo"
```

Output: `src\_Build\Release\clawPDF.exe` and `clawPDF.Settings.dll` with their
supporting assemblies/assets. The application expects compiled printer-monitor
binaries in the architecture-specific library directories; the source checkout
does not include all of these binaries. For the private test build, unchanged
monitor binaries were extracted from the bundled official 0.9.3 MSI. Rebuilding
the native monitors requires C++ tools. MSI packaging requires the Installer
Projects extension and reviewing the setup project's configuration.

**Signing:** the projects expect a private `signClawPDF.pfx` which is not in Git.
The commands above disable strong-name signing for a local source build. Rebuild
and use the managed application/dependencies together. An unsigned replacement
`clawPDF.Settings.dll` is **not** a drop-in replacement for the officially signed
installation; its assembly identity differs. A release-compatible signed hotfix
requires the upstream signing process. The original setup project also records
signed assembly identities and must be adjusted for a separately packaged build.
The upstream patch itself does not alter release packaging or signing; a private
test installer is built separately from a copy of the patched source.

You do **not** need to buy a certificate or re-sign every dependency to build a
working private version. Disabling strong-name signing consistently for the
managed source projects is one route; using your own strong-name key consistently
is another. Your own key does not reproduce the official assembly identity, so it
does not make a standalone DLL interchangeable with the official one. Unchanged
third-party assemblies and bundled printer binaries need not be re-signed.
Installer/Authenticode publisher signing is separate from .NET strong-name
identity; deployment policies may impose their own requirements. See Microsoft's
[assembly signing explanation](https://learn.microsoft.com/en-us/dotnet/standard/assembly/security-considerations).
For a normal upgrade of an existing official installation, an upstream release
containing this patch is the most straightforward distribution path.

## RDS acceptance test

Use a Windows test installation built consistently from source. As the user who
will print, write the literal REG_SZ value `\\pwrhaus\haufe\pwrhaus\dok` to
`HKCU\Software\clawSoft\clawPDF\Settings\ConversionProfiles\0\AutoSave\TargetDirectory`,
with Enabled=True and EnsureUniqueFilenames=False. Ensure the selected profile is
DefaultGuid and reload/start clawPDF. Print the PowerHaus document and check that
the resulting log/output path is
`\\pwrhaus\haufe\pwrhaus\dok\260925_AS_Journal_111930.pdf`.
Repeat with `C:\Temp`, then with the GUI-saved serialized values, and after saving
and reloading settings. Use two RDS users with different target folders to confirm
user separation. Actual share access permissions and successful PDF conversion
must be verified on Windows; they cannot be established by serializer tests.
