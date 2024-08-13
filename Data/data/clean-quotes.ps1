﻿$baseDir = "."

$dirsToClean = @(
    "$baseDir\Quote\QuoteDividend",
    "$baseDir\Quote\QuotePrice",
    "$baseDir\Quote\QuoteSplit"
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
		Get-ChildItem $dir -File | Remove-Item -Force
        Write-Host "Cleaned $dir"
    } else {
        Write-Host "Directory not found: $dir"
    }
}