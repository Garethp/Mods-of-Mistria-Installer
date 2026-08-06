param(
    [Parameter(Mandatory = $true)]
    [string]$GameLocation,
    [string]$ModsLocation = (Join-Path ([IO.Path]::GetTempPath()) 'MOMI_ui_12_mods'),
    [string]$Executable = (Join-Path $PSScriptRoot '..\ModsOfMistriaGUI\bin\Debug\net8.0\win-x64\ModsOfMistriaInstaller.exe')
)

$ErrorActionPreference = 'Stop'

$env:MOMI_GAME_LOCATION = (Resolve-Path $GameLocation).Path
$env:MOMI_MODS_LOCATION = (Resolve-Path $ModsLocation).Path
$exe = (Resolve-Path $Executable).Path

Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
