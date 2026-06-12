$env:DOTNET_CLI_HOME = "d:\SPC\.dotnet_home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = "C:\Users\ihao_ting.PMR.000\.nuget\packages"
Write-Host "Restoring backend API..."
dotnet restore .\backend\MesSpc.Api\MesSpc.Api.csproj --source "C:\Users\ihao_ting.PMR.000\.nuget\packages"
Write-Host "Building backend API..."
dotnet build .\backend\MesSpc.Api\MesSpc.Api.csproj --no-restore
