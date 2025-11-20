# Fix TargetFramework format in all csproj files
$csprojFiles = Get-ChildItem -Path . -Recurse -Filter *.csproj

foreach ($file in $csprojFiles) {
    Write-Host "Processing file: $($file.FullName)"
    
    # Read file content
    $content = Get-Content -Path $file.FullName -Raw
    
    # Fix net8\.0 to net8.0
    if ($content -match '<TargetFramework>net8\\\.0</TargetFramework>') {
        $newContent = $content -replace '<TargetFramework>net8\\\.0</TargetFramework>', '<TargetFramework>net8.0</TargetFramework>'
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "  Fixed TargetFramework format"
    }
    # Fix net8\.0-windows to net8.0-windows
    elseif ($content -match '<TargetFramework>net8\\\.0-windows</TargetFramework>') {
        $newContent = $content -replace '<TargetFramework>net8\\\.0-windows</TargetFramework>', '<TargetFramework>net8.0-windows</TargetFramework>'
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "  Fixed TargetFramework format"
    }
    else {
        Write-Host "  No TargetFramework format issues found"
    }
}

Write-Host ""
Write-Host "Fix completed!"
