param
(
    [ValidateSet("Debug", "Release")]
    [String[]]$Configurations = @("Debug", "Release")
)

Push-Location $PSScriptRoot
foreach ($config in $Configurations)
{
    dotnet restore PKHeX.sln /p:Configuration=$config /p:Platform="Any CPU"
    MSBuild.exe PKHeX.WinForms/PKHeX.WinForms.csproj /p:Configuration=$config

    Push-Location "PKHeX.WinForms/bin/$config/net10.0-windows/win-x64"
    robocopy "."    "plugins\"  "AutoModPlugins.dll"
    $PKHeXPlugins = $lastexitcode
    
    robocopy "."    "plugins\"  "HomeLive.Plugins.dll"
    $HOMELivePlugin = $lastexitcode
    Pop-Location

    if ($PKHeXPlugins -ne 1 -or $HOMELivePlugin -ne 1)
    {
      exit 1
    }
    else
    {
      exit 0
    }
}
Pop-Location