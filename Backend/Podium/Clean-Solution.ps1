# Clean-Solution.ps1
# Deletes bin, obj, and .vs folders to force a complete rebuild

param(
    [string]$SolutionPath = "C:\Users\josep\source\DocumentManagementSystem\Backend\Podium"
)

Write-Host "Cleaning solution at: $SolutionPath" -ForegroundColor Cyan
Write-Host ""

# Check if path exists
if (-not (Test-Path $SolutionPath)) {
    Write-Host "ERROR: Solution path not found: $SolutionPath" -ForegroundColor Red
    exit 1
}

# Function to remove folder with error handling
function Remove-FolderSafely {
    param([string]$Path)
    
    if (Test-Path $Path) {
        try {
            Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
            Write-Host "✓ Deleted: $Path" -ForegroundColor Green
        }
        catch {
            Write-Host "✗ Failed to delete: $Path" -ForegroundColor Yellow
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

# Delete .vs folder (hidden folder in solution root)
$vsFolder = Join-Path $SolutionPath ".vs"
Remove-FolderSafely $vsFolder

# Find and delete all bin and obj folders
$foldersToDelete = Get-ChildItem -Path $SolutionPath -Include bin,obj -Recurse -Directory -Force

$count = 0
foreach ($folder in $foldersToDelete) {
    Remove-FolderSafely $folder.FullName
    $count++
}

Write-Host ""
Write-Host "Cleanup complete! Deleted $count bin/obj folders." -ForegroundColor Cyan
Write-Host "You can now reopen Visual Studio and rebuild the solution." -ForegroundColor Cyan