@echo off
echo Running Entity Framework Migrations...
dotnet ef migrations add AddRecheckAdjustFields
echo.
echo Updating Database...
dotnet ef database update
echo.
pause
