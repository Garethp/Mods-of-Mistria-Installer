param(
    [string]$Source = (Join-Path ([IO.Path]::GetTempPath()) 'MOMI_ui_12_mods'),
    [string]$ProfileDestination = (Join-Path ([IO.Path]::GetTempPath()) 'MOMI_ui_profiles_test'),
    [string]$CleanDestination = (Join-Path ([IO.Path]::GetTempPath()) 'MOMI_ui_clean_test')
)

foreach ($destination in @($ProfileDestination, $CleanDestination)) {
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Get-ChildItem -LiteralPath $destination -Directory | Remove-Item -Recurse -Force
    Get-ChildItem -LiteralPath $destination -File | Remove-Item -Force
}

$modDirectories = Get-ChildItem -LiteralPath $Source -Directory
foreach ($destination in @($ProfileDestination, $CleanDestination)) {
    foreach ($mod in $modDirectories) {
        Copy-Item -LiteralPath $mod.FullName -Destination $destination -Recurse
    }
}

$ids = 1..12 | ForEach-Object { "momi_test_suite.ui_test_mod_$($_.ToString('00'))" }
$profiles = [ordered]@{
    Default = [ordered]@{
        enabledMods = @($ids)
        loadOrder = @($ids)
    }
    'Second Profile' = [ordered]@{
        enabledMods = @($ids)
        loadOrder = @($ids[11], $ids[10]) + @($ids[0..9])
    }
}

[ordered]@{
    currentProfile = 'Second Profile'
    profiles = $profiles
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ProfileDestination 'momi_profiles.json') -Encoding utf8

Write-Output "Profile test: $ProfileDestination"
Write-Output "Clean test:   $CleanDestination"
