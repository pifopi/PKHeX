for %%X in (Debug Release) do (
    pushd %~dp0
    dotnet restore /p:Configuration=%%X /p:Platform="Any CPU"
    MSBuild.exe PKHeX.WinForms/PKHeX.WinForms.csproj /p:Configuration=%%X

    cd ../PKHeX-Plugins
    dotnet restore /p:Configuration=%%X /p:Platform="Any CPU"
    MSBuild.exe AutoLegalityMod/AutoModPlugins.csproj /p:Configuration=%%X
    robocopy AutoLegalityMod\bin\%%X\net8.0-windows ..\PKHeX\PKHeX.WinForms\bin\%%X\net8.0-windows\win-x64\plugins\ AutoModPlugins.dll LibUsbDotNet.LibUsbDotNet.dll NtrSharp.dll PKHeX.Core.AutoMod.dll PKHeX.Core.Enhancements.dll PKHeX.Core.Injection.dll
    popd
)
