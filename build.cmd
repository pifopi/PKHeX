for %%X in (Debug Release) do (
    pushd %~dp0
    dotnet restore PKHeX.sln /p:Configuration=%%X /p:Platform="Any CPU"
    MSBuild.exe PKHeX.WinForms/PKHeX.WinForms.csproj /p:Configuration=%%X

    cd ../PKHeX-Plugins
    dotnet restore /p:Configuration=%%X /p:Platform="Any CPU"
    MSBuild.exe AutoLegalityMod/AutoModPlugins.csproj /p:Configuration=%%X
    robocopy AutoLegalityMod\bin\%%X\net10.0-windows ..\PKHeX\PKHeX.WinForms\bin\%%X\net10.0-windows\win-x64\plugins\ AutoModPlugins.dll

    cd ../HOME-Live-Plugin
    dotnet restore /p:Configuration=%%X /p:Platform="Any CPU"
    MSBuild.exe HomeLive.Plugins/HomeLive.Plugins.csproj /p:Configuration=%%X
    robocopy HomeLive.Plugins\bin\%%X\net10.0-windows7.0 ..\PKHeX\PKHeX.WinForms\bin\%%X\net10.0-windows\win-x64\plugins\ HomeLive.Plugins.dll

    popd
)
