#Set-StrictMode -Version Latest

#$ErrorActionPreference = 'Stop'
#WarningPreference = 'Stop'

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
        [string]$Task,
        [string]$Project
    )
    switch ($Task) {
        "build" {
            Execute-Command "Building project/solution" "dotnet build $Project --configuration $Configuration"
        }
        "clean" {
            Execute-Command "Cleaning project/solution" "dotnet clean $Project --configuration $Configuration"
            Execute-Command "Removing bin and obj directories" "Get-ChildItem -Path . -Include bin, obj -Recurse | Remove-Item -Recurse -Force"
        }
        "run" {
            Execute-Command "Launching project" "dotnet run --project $Project --configuration $Configuration --no-build"
        }
        "run:DataRefreshJob" {
            Execute-Task "run" "DataRefreshJob\\DataRefreshJob.csproj"
        }
        "run:WebApp.Server" {
            Execute-Task "run" "WebApp\\WebApp.Server\\WebApp.Server.csproj --launch-profile https"
        }
        "test" {
            Execute-Command "Running project/solution tests" "dotnet test $Project --configuration $Configuration --no-build"
        }
        default {
            Write-Warning "Unrecognized task `"$($Task)`"." -ForegroundColor Yellow
        }
    }
}

function Main {
    $TaskList = $Tasks -split ',' | ForEach-Object { $_.Trim() }
    foreach ($Task in $TaskList) {
        Execute-Task $Task $ProjectOrSolution
    }
}

if (-not $Tasks) {
    Show-Usage
} else {
    Main
}