$env:DOTNET_CLI_HOME = "d:\SPC\.dotnet_home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = "C:\Users\ihao_ting.PMR.000\.nuget\packages"
Write-Host "Running dotnet restore with local source..."
dotnet restore .\tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --source "C:\Users\ihao_ting.PMR.000\.nuget\packages"
Write-Host "Running dotnet test..."
dotnet test .\tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --no-restore
