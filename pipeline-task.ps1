param (
    [Parameter(Position=0, Mandatory=$true)]
    [string]$Task,

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

    Write-Host "$($Task): $($Description)_" -ForegroundColor Cyan
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
.\pipeline-task.ps1 [Task] [SolutionFile] [Configuration]

Task: clean, build, run, or test (required)
SolutionFile: Path to the solution file (optional, default: PortfolioAnalyzer.sln)
Configuration: Debug or Release (optional, default: Debug)

Examples:
.\pipeline-task.ps1 build
.\pipeline-task.ps1 build MySpecificSolution.sln Release
.\pipeline-task.ps1 test
.\pipeline-task.ps1 run PortfolioAnalyzer.sln Release
"@ -ForegroundColor Cyan
}

function Main {
    switch ($Task) {
        "clean" {
            Execute-Command "Cleaning project/solution" "dotnet clean $ProjectOrSolution --configuration $Configuration"
            Execute-Command "Removing bin and obj directories" "Get-ChildItem -Path . -Include bin, obj -Recurse | Remove-Item -Recurse -Force"
        }
        "build" {
            Execute-Command "Building project/solution" "dotnet build $ProjectOrSolution --configuration $Configuration"
        }
        "run:WebApp.Server" {
            Execute-Command "Launching project" "dotnet run --project WebApp\\WebApp.Server\\WebApp.Server.csproj --configuration $Configuration --no-build --launch-profile 'https'"
        }
        "run:WebApp.ClientLegacy" {
            Execute-Command "Launching project" "dotnet run --project **/WebApp.ClientLegacy.esproj --configuration $Configuration --no-build --launch-profile 'Web Service'"
        }
        "run:DataRefreshJob" {
            Execute-Command "Launching project" "dotnet run --project **/DataRefreshJob.csproj --configuration $Configuration --no-build --launch-profile 'Web Service'"
        }
        "test" {
            Execute-Command "Running project/solution tests" "dotnet test $ProjectOrSolution --configuration $Configuration --no-build"
        }
        default {
            Write-Warning "Unrecognized task `"$($Task)`"." -ForegroundColor Cyan
            Show-Usage
            exit 1
        }
    }
}

if (-not $Task) {
    Show-Usage
} else {
    Main
}