param(
    [string]$Destination = (Join-Path ([IO.Path]::GetTempPath()) 'MOMI_ui_12_mods')
)

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

1..12 | ForEach-Object {
    $number = $_.ToString('00')
    $folder = Join-Path $Destination "UI Test Mod $number"
    New-Item -ItemType Directory -Force -Path $folder | Out-Null

    $manifest = [ordered]@{
        name = "UI Test Mod $number"
        author = 'MOMI Test Suite'
        version = '1.0.0'
        description = "Harmless UI list test fixture $number."
        minInstallerVersion = '0.15.1'
        manifestVersion = 1
        requires_hooks = @()
    }

    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'manifest.json') -Encoding utf8
    Set-Content -LiteralPath (Join-Path $folder 'README.txt') -Value "Harmless UI test fixture $number. Do not install into the live game." -Encoding utf8
}

Write-Output "Created 12 isolated UI test mods in $Destination"
