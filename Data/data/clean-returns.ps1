$baseDir = "."

$dirsToClean = @(
    "$baseDir\PeriodReturn\Daily",
    "$baseDir\PeriodReturn\Monthly",
    "$baseDir\PeriodReturn\Yearly"
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
        if ($dir -like "*\PeriodReturn\*") {
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