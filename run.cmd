@echo off
echo 1. Triangle
echo 2. Circle
echo 3. Animation2D
echo 4. DepthBuffer
echo 5. PerspectiveProjection
choice /c 12345 /n /m "Select a step [1/2/3/4/5]: "
if errorlevel 5 (
    set "taskProject=Step5_PerspectiveProjection\Step5_PerspectiveProjection.csproj"
) else if errorlevel 4 (
    set "taskProject=Step4_DepthBuffer\Step4_DepthBuffer.csproj"
) else if errorlevel 3 (
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
