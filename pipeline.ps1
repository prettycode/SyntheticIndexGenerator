param(
    [Parameter(Mandatory=$true)]
    [string]$Task,

    [Parameter(Mandatory=$false)]
    [string]$SolutionFile = "*.sln",

    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Debug"
)

function Build {
    Write-Host "Building the solution in $Configuration configuration..."
    dotnet build $SolutionFile --configuration $Configuration
}

function Test {
    Write-Host "Running tests in $Configuration configuration..."
    dotnet test $SolutionFile --configuration $Configuration --no-build
}

function Run {
    Write-Host "Launching the solution in $Configuration configuration..."
    $projectFiles = Get-ChildItem -Filter *.csproj -Recurse
    foreach ($projectFile in $projectFiles) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
        Write-Host "Launching project: $projectName"
        Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$($projectFile.FullName)`" --configuration $Configuration --no-build" -NoNewWindow
    }
}

switch ($Task.ToLower()) {
    "build" { Build }
    "test" { Test }
    "run" { Run }
    default { Write-Host "Invalid task. Available tasks are: build, test, run" }
}

# .\build-pipeline.ps1 -Task build
# .\build-pipeline.ps1 -Task build -Configuration Release
# .\build-pipeline.ps1 -Task test
# .\build-pipeline.ps1 -Task run -Configuration Release
# .\build-pipeline.ps1 -Task build -SolutionFile "MySpecificSolution.sln" -Configuration Release
