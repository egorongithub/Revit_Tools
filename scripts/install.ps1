#Requires -Version 5.1
<#
.SYNOPSIS
    Сборка и установка Sminex BIM Tools для Revit 2022 и 2024.

.DESCRIPTION
    Собирает плагин в конфигурации Release для каждой указанной версии Revit
    и копирует DLL вместе с манифестом .addin в папку надстроек текущего
    пользователя: %AppData%\Autodesk\Revit\Addins\<версия>.

.EXAMPLE
    .\install.ps1                     # собрать и установить для Revit 2022 и 2024
    .\install.ps1 -RevitVersions 2024 # только для Revit 2024
#>
param(
    [string[]] $RevitVersions = @('2022', '2024'),
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptsDir
$project = Join-Path $repoRoot 'src\SminexBimTools\SminexBimTools.csproj'
$addinManifest = Join-Path $repoRoot 'addin\SminexBimTools.addin'

foreach ($version in $RevitVersions) {
    $config = "$Configuration R$version"
    Write-Host "==> Сборка: $config" -ForegroundColor Cyan

    dotnet build $project -c $config
    if ($LASTEXITCODE -ne 0) {
        throw "Сборка для Revit $version завершилась с ошибкой."
    }

    $binDir = Join-Path $repoRoot "src\SminexBimTools\bin\$config"
    $addinsDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$version"
    $targetDir = Join-Path $addinsDir 'SminexBimTools'

    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    Copy-Item (Join-Path $binDir 'SminexBimTools.dll') $targetDir -Force
    Copy-Item (Join-Path $binDir 'SminexBimTools.pdb') $targetDir -Force -ErrorAction SilentlyContinue
    Copy-Item $addinManifest $addinsDir -Force

    Write-Host "==> Установлено для Revit ${version}: $targetDir" -ForegroundColor Green
}

Write-Host ''
Write-Host 'Готово. Запустите Revit — вкладка «Sminex BIM Tools» появится на ленте.' -ForegroundColor Green
