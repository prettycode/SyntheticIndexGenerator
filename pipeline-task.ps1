param (
    [Parameter(Position=0, Mandatory=$true)]
    [string]$Tasks,
    [Parameter(Position=1, Mandatory=$false)]
    [string]$ProjectOrSolution = "PortfolioAnalyzer.sln",
    [Parameter(Position=2, Mandatory=$false)]
    [string]$Configuration = "Debug"
)

function Execute-Command {
    param (
        [string]$Description,
        [string]$Command
    )
    Write-Host "$($Task): $($Description)..." -ForegroundColor Cyan
    Write-Host "> $Command"
    Invoke-Expression $Command
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Command failed with exit code $LASTEXITCODE"
        exit 1
    }
}

function Show-Usage {
    Write-Host @"
Usage:
.\pipeline-task.ps1 [Tasks] [SolutionFile] [Configuration]
Tasks: Comma-separated list of tasks to execute (clean, build, test, run, run:WebApp.Server, run:WebApp.ClientLegacy, run:DataRefreshJob) (required)
SolutionFile: Path to the solution file (optional, default: PortfolioAnalyzer.sln)
Configuration: Debug or Release (optional, default: Debug)
Examples:
.\pipeline-task.ps1 "run" MyProject.csproj
.\pipeline-task.ps1 "build, test" MySpecificSolution.sln Release
.\pipeline-task.ps1 "clean, build, test, run"
.\pipeline-task.ps1 "run:WebApp.Server, run:WebApp.ClientLegacy"
"@ -ForegroundColor Cyan
}

function Execute-Task {
    param (
        [string]$Task
    )
    switch ($Task) {
        "clean" {
            Execute-Command "Cleaning project/solution" "dotnet clean $ProjectOrSolution --configuration $Configuration"
            Execute-Command "Removing bin and obj directories" "Get-ChildItem -Path . -Include bin, obj -Recurse | Remove-Item -Recurse -Force"
        }
        "build" {
            Execute-Command "Building project/solution" "dotnet build $ProjectOrSolution --configuration $Configuration"
        }
        "test" {
            Execute-Command "Running project/solution tests" "dotnet test $ProjectOrSolution --configuration $Configuration --no-build"
        }
        "run" {
            Execute-Command "Launching project" "dotnet run --project $ProjectOrSolution --configuration $Configuration --no-build"
        }
        "run:WebApp.Server" {
            $ProjectOrSolution = "WebApp\WebApp.Server\WebApp.Server.csproj --launch-profile https"
            Execute-Command "Launching WebApp.Server" "dotnet run --project $ProjectOrSolution --configuration $Configuration --no-build"
        }
        "run:WebApp.ClientLegacy" {
            $ProjectOrSolution = "WebApp.ClientLegacy\WebApp.ClientLegacy.esproj"
            Execute-Command "Launching WebApp.ClientLegacy" "dotnet run --project $ProjectOrSolution --configuration $Configuration --no-build"
        }
        "run:DataRefreshJob" {
            $ProjectOrSolution = "DataRefreshJob\DataRefreshJob.csproj"
            Execute-Command "Launching DataRefreshJob" "dotnet run --project $ProjectOrSolution --configuration $Configuration --no-build"
        }
        default {
            Write-Warning "Unrecognized task `"$($Task)`"." -ForegroundColor Yellow
        }
    }
}

function Main {
    $TaskList = $Tasks -split ',' | ForEach-Object { $_.Trim() }
    foreach ($Task in $TaskList) {
        Execute-Task $Task
    }
}

if (-not $Tasks) {
    Show-Usage
} else {
    Main
}