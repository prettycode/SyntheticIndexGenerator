build:
    dotnet build
clean:
    dotnet clean
restore:
    dotnet restore
test:

watch:
    dotnet watch --project **/DataRefreshJob.csproj run
start:
    dotnet run --project **/DataRefreshJob.csproj