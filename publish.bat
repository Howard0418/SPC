@echo off
chcp 65001 >nul
echo ===================================================
echo   MES-SPC 系統一鍵發佈腳本 (One-Click Publish)
echo ===================================================
echo.

set ROOT_DIR=%~dp0
set PUBLISH_DIR=%ROOT_DIR%publish

echo [1/3] 準備發佈資料夾...
if exist "%PUBLISH_DIR%\frontend" rd /s /q "%PUBLISH_DIR%\frontend"
if exist "%PUBLISH_DIR%\backend" rd /s /q "%PUBLISH_DIR%\backend"
mkdir "%PUBLISH_DIR%\frontend"
mkdir "%PUBLISH_DIR%\backend"

echo.
echo [2/3] 開始編譯前端 (Vue 3 + Vite)...
cd /d "%ROOT_DIR%frontend\mes-spc-web"
call npm install
call npm run build
if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ 前端編譯失敗，請檢查錯誤訊息！
    pause
    exit /b %ERRORLEVEL%
)
xcopy /E /I /Y "dist\*" "%PUBLISH_DIR%\frontend\" >nul
echo ✅ 前端編譯與複製完成！

echo.
echo [3/3] 開始發佈後端 API (.NET 10)...
cd /d "%ROOT_DIR%backend\MesSpc.Api"
dotnet publish -c Release -o "%PUBLISH_DIR%\backend"
if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ 後端發佈失敗，請檢查錯誤訊息！
    pause
    exit /b %ERRORLEVEL%
)
echo ✅ 後端發佈完成！

echo.
echo ===================================================
echo  🎉 發佈大功告成！
echo.
echo  IIS 前端站台請指向: %PUBLISH_DIR%\frontend
echo  IIS 後端應用請指向: %PUBLISH_DIR%\backend
echo ===================================================
pause
