$baseDir = "..\Data\data"

$dirsToClean = @(
    "$baseDir\quotes\dvidiends",
    "$baseDir\quotes\prices",
    "$baseDir\quotes\splits"
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
        if ($dir -like "*\returns\*") {
            # For returns directories, exclude files starting with "#"
            Get-ChildItem $dir -File | Where-Object { $_.Name -notlike "#*" } | Remove-Item -Force
        } else {
            # For other directories, remove all files
            Get-ChildItem $dir -File | Remove-Item -Force
        }
        Write-Host "Cleaned $dir"
    } else {
        Write-Host "Directory not found: $dir"
    }
}