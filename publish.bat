@echo off
echo =======================================
echo MES Project One-Click Publish Script
echo =======================================

set BACKEND_PUBLISH_DIR=C:\Users\ihao_ting.PMR.000\Desktop\MES\publish\MesSpcApi
set FRONTEND_PUBLISH_DIR=C:\Users\ihao_ting.PMR.000\Desktop\MES\publish\MesSpcWeb

echo.
echo [1/2] Publishing Backend API...
echo ---------------------------------------
dotnet publish .\backend\MesSpc.Api\MesSpc.Api.csproj -c Release -o %BACKEND_PUBLISH_DIR%
if %ERRORLEVEL% neq 0 (
    echo [Error] Backend publish failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Publishing Frontend Web...
echo ---------------------------------------
cd .\frontend\mes-spc-web
echo Installing npm packages...
call npm install
echo Building frontend project...
call npm run build
if %ERRORLEVEL% neq 0 (
    echo [Error] Frontend build failed!
    cd ..\..
    pause
    exit /b %ERRORLEVEL%
)

echo Copying frontend files to %FRONTEND_PUBLISH_DIR%...
if not exist "%FRONTEND_PUBLISH_DIR%" mkdir "%FRONTEND_PUBLISH_DIR%"
xcopy /s /y /q ".\dist\*" "%FRONTEND_PUBLISH_DIR%\"

cd ..\..

echo.
echo =======================================
echo Publish Completed Successfully!
echo Backend location: %BACKEND_PUBLISH_DIR%
echo Frontend location: %FRONTEND_PUBLISH_DIR%
echo =======================================
pause
