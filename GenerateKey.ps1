[CmdletBinding()]
param()

$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)

Write-Host "Your new AES-256 key (Base64):"
Write-Host $key