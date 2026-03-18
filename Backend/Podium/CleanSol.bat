@echo off
echo Cleaning Visual Studio cache folders...
echo.

cd /d "C:\Users\josep\source\Podium\Backend\Podium"

echo Deleting .vs folder...
if exist ".vs" (
    rmdir /s /q ".vs"
    echo   [DONE] .vs folder deleted
) else (
    echo   [SKIP] .vs folder not found
)

echo.
echo Deleting bin and obj folders...
for /d /r %%d in (bin,obj) do (
    if exist "%%d" (
        rmdir /s /q "%%d"
        echo   [DONE] %%d
    )
)

echo.
echo Cleanup complete! You can now rebuild the solution.
pause