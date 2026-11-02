@echo off
dotnet run --project "%~dp0Step1_Triangle\Step1_Triangle.csproj" -c Release
if errorlevel 1 (
    pause
    exit /b 1
)
