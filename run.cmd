@echo off
echo 1. Triangle
echo 2. Circle
echo 3. Animation2D
choice /c 123 /n /m "Select a step [1/2/3]: "
if errorlevel 3 (
    set "taskProject=Step3_Animation2D\Step3_Animation2D.csproj"
) else if errorlevel 2 (
    set "taskProject=Step2_Circle\Step2_Circle.csproj"
) else (
    set "taskProject=Step1_Triangle\Step1_Triangle.csproj"
)
dotnet run --project "%~dp0%taskProject%" -c Release
if errorlevel 1 (
    pause
    exit /b 1
)
