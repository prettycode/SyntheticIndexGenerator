param(
    [Parameter(Mandatory=$true)]
    [string]$Task,

    [Parameter(Mandatory=$false)]
    [string]$SolutionFile = "PortfolioAnalyzer.sln",

    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Debug"
)

function Write-Command {
    param([string]$Command)
    Write-Host "> $Command" -ForegroundColor Cyan
}

function Build {
    Write-Host "Building the solution in $Configuration configuration..." -ForegroundColor Green
    $buildCommand = "dotnet build $SolutionFile --configuration $Configuration"
    Write-Command $buildCommand
    Invoke-Expression $buildCommand
}

function Test {
    Write-Host "Running tests in $Configuration configuration..." -ForegroundColor Green
    $testCommand = "dotnet test $SolutionFile --configuration $Configuration --no-build"
    Write-Command $testCommand
    Invoke-Expression $testCommand
}

function Run {
    Write-Host "Launching the solution in $Configuration configuration..." -ForegroundColor Green
    $projectFiles = Get-ChildItem -Filter *.csproj -Recurse
    foreach ($projectFile in $projectFiles) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
        Write-Host "Launching project: $projectName" -ForegroundColor Yellow
        $runCommand = "dotnet run --project `"$($projectFile.FullName)`" --configuration $Configuration --no-build"
        Write-Command $runCommand
        Start-Process -FilePath "dotnet" -ArgumentList $runCommand.Substring(7) -NoNewWindow
    }
}

switch ($Task.ToLower()) {
    "build" { Build }
    "test" { Test }
    "run" { Run }
    default { Write-Host "Invalid task. Available tasks are: build, test, run" -ForegroundColor Red }
}

# .\pipeline.ps1 -Task build
# .\pipeline.ps1 -Task build -Configuration Release
# .\pipeline.ps1 -Task test
# .\pipeline.ps1 -Task run -Configuration Release
# .\pipeline.ps1 -Task build -SolutionFile "MySpecificSolution.sln" -Configuration Release
