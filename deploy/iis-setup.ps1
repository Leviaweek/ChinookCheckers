# Publishes ChinookCheckers.Api to IIS: app pool (No Managed Code, x64), site, folder permissions.
# Run from an elevated PowerShell on the Windows server. Needs IIS and the .NET 10 Hosting Bundle installed.
param(
    [string]$Name = "ChinookCheckers",
    [int]$Port = 8081,
    [string]$Dir = "C:\inetpub\ChinookCheckers",
    [string]$EngineDir = "C:\Kingsrow\engines",   # the pool writes the Kingsrow64.worker*.dll copies here
    [string]$DbDir = "C:\kr_english_wld"
)
Import-Module WebAdministration
$identity = "IIS AppPool\$Name"

if (Test-Path "IIS:\Sites\$Name") { Remove-Website $Name }
if (Test-Path "IIS:\AppPools\$Name") { Remove-WebAppPool $Name }

dotnet publish "$PSScriptRoot\..\ChinookCheckers.Api" -c Release -o $Dir
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

New-WebAppPool $Name | Out-Null
Set-ItemProperty "IIS:\AppPools\$Name" managedRuntimeVersion ""        # .NET brings its own runtime
Set-ItemProperty "IIS:\AppPools\$Name" enable32BitAppOnWin64 $false     # KingsRow64.dll is 64-bit
Set-ItemProperty "IIS:\AppPools\$Name" startMode AlwaysRunning          # warm the engines without waiting for a request
Set-ItemProperty "IIS:\AppPools\$Name" processModel.idleTimeout "00:00:00"
New-Website $Name -Port $Port -PhysicalPath $Dir -ApplicationPool $Name | Out-Null

# The app only needs to read its own folder, but writes the engine's profile and its logs.
foreach ($d in "profile", "logs") { New-Item -ItemType Directory -Force "$Dir\$d" | Out-Null }
icacls $Dir /grant "${identity}:(OI)(CI)RX" | Out-Null
foreach ($d in "profile", "logs") { icacls "$Dir\$d" /grant "${identity}:(OI)(CI)M" | Out-Null }
icacls $EngineDir /grant "${identity}:(OI)(CI)M" | Out-Null
icacls $DbDir /grant "${identity}:(OI)(CI)RX" | Out-Null

Start-Website $Name
"Site '$Name' on http://localhost:$Port/  pool=$((Get-WebAppPoolState $Name).Value)  (first start takes about a minute)"
