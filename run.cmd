@echo off
echo 1. Triangle
echo 2. Circle
choice /c 12 /n /m "Select a step [1/2]: "
if errorlevel 2 (
    set "taskProject=Step2_Circle\Step2_Circle.csproj"
) else (
    set "taskProject=Step1_Triangle\Step1_Triangle.csproj"
)
dotnet run --project "%~dp0%taskProject%" -c Release
if errorlevel 1 (
    pause
    exit /b 1
)
