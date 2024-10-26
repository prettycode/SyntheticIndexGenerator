# clean-all.ps1
# This script executes all other "clean-*.ps1" scripts in the same directory, except itself.

# Get the directory of the current script
$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Find all "clean-*.ps1" scripts excluding "clean-all.ps1"
$Scripts = Get-ChildItem -Path $ScriptDirectory -Filter "clean-*.ps1" | Where-Object { $_.Name -ne "clean-all.ps1" }

# Execute each script
foreach ($script in $Scripts) {
    Write-Host "Executing $($script.Name)..."
    try {
        & $script.FullName
    }
    catch {
        Write-Host "Error executing $($script.Name): $_"
    }
}
