# pipeline.ps1

$ErrorActionPreference = "Stop"

function Invoke-PipelineTask {
    param (
        [string]$Task
    )

    Write-Host "Executing task '$Task'" -ForegroundColor Cyan

    try {
        & .\pipeline-task.ps1 $Task

        if ($LASTEXITCODE -ne 0) {
            throw "Task '$Task' failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Error "Error executing task '$Task': $_"
        exit 1
    }

    Write-Host "Task '$Task' completed successfully" -ForegroundColor Green
}

$tasks = @(
    "clean",
    "build",
    "test"
)

foreach ($task in $tasks) {
    Invoke-PipelineTask $task
}

Write-Host "All tasks completed successfully" -ForegroundColor Green