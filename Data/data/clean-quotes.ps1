$baseDir = "."

$dirsToClean = @(
    "$baseDir\QuotePrice"
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
		Get-ChildItem $dir -File | Remove-Item -Force
        Write-Host "Cleaned $dir"
    } else {
        Write-Host "Directory not found: $dir"
    }
}