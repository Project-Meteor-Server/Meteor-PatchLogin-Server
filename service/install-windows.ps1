# Installs the web server as a Windows service that starts at boot. Run in an admin PowerShell:
#   .\service\install-windows.ps1              install or update
#   .\service\install-windows.ps1 uninstall    remove the service (keeps the folder)
param([string]$Action = "install")
$ErrorActionPreference = "Stop"
$Name = "MeteorWeb"
$Dir = Join-Path $env:ProgramFiles "MeteorWeb"
$Repo = Split-Path $PSScriptRoot -Parent

if ($Action -eq "uninstall") {
    if (Get-Service $Name -ErrorAction SilentlyContinue) {
        Stop-Service $Name -ErrorAction SilentlyContinue
        sc.exe delete $Name | Out-Null
    }
    Write-Host "Removed the service. $Dir is still there."
    exit 0
}

$existing = Get-Service $Name -ErrorAction SilentlyContinue
if ($existing) { Stop-Service $Name -ErrorAction SilentlyContinue }
dotnet publish (Join-Path $Repo "WebServer") -c Release -o $Dir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
# Keep an existing config: it holds your database password.
$config = Join-Path $Dir "web_config.ini"
if (-not (Test-Path $config)) { Copy-Item (Join-Path $Repo "WebServer\web_config.ini") $config }

if (-not $existing) {
    New-Service -Name $Name -BinaryPathName "`"$(Join-Path $Dir 'WebServer.exe')`"" `
        -DisplayName "Project Meteor web server" -StartupType Automatic | Out-Null
}
Start-Service $Name
Write-Host "Installed. Settings: $config  Logs: $(Join-Path $Dir 'logs')"
