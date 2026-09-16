@echo off
setlocal
set "COURSE_DIR=%~dp0"
for %%I in ("%COURSE_DIR%..") do set "REPO_ROOT=%%~fI"
echo Demarrage du cours local hors ligne...
dotnet run --project "%COURSE_DIR%server\CourseServer.csproj" -- "%REPO_ROOT%"
endlocal
