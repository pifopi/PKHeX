param
(
    [ValidateSet("Debug", "Release")]
    [String[]]$Configurations = @("Debug", "Release")
)

Push-Location $PSScriptRoot
foreach ($config in $Configurations)
{
    dotnet restore PKHeX.slnx /p:Configuration=$config /p:Platform="Any CPU"
    MSBuild.exe PKHeX.WinForms/PKHeX.WinForms.csproj /p:Configuration=$config

    Push-Location "PKHeX.WinForms/bin/$config/net10.0-windows/win-x64"
    New-Item -ItemType Directory -Path "plugins" -Force
    Copy-Item "AutoModPlugins.dll" -Destination "plugins/AutoModPlugins.dll"
    Copy-Item "HomeLive.Plugins.dll" -Destination "plugins/HomeLive.Plugins.dll"
    Pop-Location
}
Pop-Location