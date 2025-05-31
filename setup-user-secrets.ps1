# setup-user-secrets.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Function to generate MasterKey
function Generate-MasterKey {
    $bytes = New-Object byte[] 32
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $key = [Convert]::ToBase64String($bytes)
    return $key
}

# Check for dotnet CLI
try {
    dotnet --version | Out-Null
    Write-Host "dotnet CLI found."
}
catch {
    Write-Error "dotnet CLI not found. Please install the .NET SDK."
    exit 1
}

$scriptPath = $PSScriptRoot
$templatePath = Join-Path $scriptPath "secrets.template.json"
$serverPath = Join-Path $scriptPath "GateKeeper.Server"

if (-not (Test-Path $templatePath)) {
    Write-Error "secrets.template.json not found in script directory."
    Write-Host "Please create it based on GateKeeper.Server/UserSecret.Example and fill in your secret values."
    exit 1
}

# Read secrets from template
$secrets = Get-Content $templatePath | ConvertFrom-Json

# Generate MasterKey
$masterKey = Generate-MasterKey
Write-Host "Generated MasterKey: $masterKey"

# Add/replace MasterKey in the secrets object
# Using KeyManagement:MasterKey as per README's Current Secret Configuration Structure
if ($secrets.PSObject.Properties["KeyManagement"]) {
    $secrets.KeyManagement.MasterKey = $masterKey
} else {
    $secrets | Add-Member -MemberType NoteProperty -Name "KeyManagement" -Value ([PSCustomObject]@{ MasterKey = $masterKey })
}


# Navigate to GateKeeper.Server
try {
    Set-Location $serverPath
    Write-Host "Changed directory to $(Get-Location)"
}
catch {
    Write-Error "Failed to navigate to GateKeeper.Server directory: $serverPath"
    exit 1
}

# Initialize user secrets
try {
    dotnet user-secrets init | Out-Null
    Write-Host "User secrets initialized for GateKeeper.Server."
}
catch {
    Write-Error "Failed to initialize user secrets. Error: $($_.Exception.Message)"
    # Continue script execution as secrets might already be initialized
}

# Set secrets
Write-Host "Setting user secrets..."
$secrets.PSObject.Properties | ForEach-Object {
    $section = $_.Name
    $_.Value.PSObject.Properties | ForEach-Object {
        $key = $_.Name
        $value = $_.Value
        if ($value -ne $null -and $value -ne "") {
            try {
                dotnet user-secrets set "$section:$key" "$value"
                Write-Host "Set secret: $section:$key"
            }
            catch {
                Write-Warning "Failed to set secret: $section:$key. Error: $($_.Exception.Message)"
            }
        } else {
            Write-Warning "Skipping empty value for secret: $section:$key"
        }
    }
}

Write-Host "User secrets setup complete."
Set-Location $scriptPath # Return to original directory
