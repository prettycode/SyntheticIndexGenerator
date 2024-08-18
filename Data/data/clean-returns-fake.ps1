$baseDir = "."

$dirsToClean = @(
    "$baseDir\PeriodReturn\Daily",
    "$baseDir\PeriodReturn\Monthly",
    "$baseDir\PeriodReturn\Yearly"
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
		# Exclude files starting with "#"
		Get-ChildItem $dir -File | Where-Object { $_.Name -like "#*" } | Remove-Item -Force
        Write-Host "Cleaned $dir"
    } else {
        Write-Host "Directory not found: $dir"
    }
}