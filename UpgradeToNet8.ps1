# Upgrade all .NET projects to .NET 8
$csprojFiles = Get-ChildItem -Path . -Recurse -Filter *.csproj

foreach ($file in $csprojFiles) {
    Write-Host "Processing file: $($file.FullName)"
    
    # Read file content
    $content = Get-Content -Path $file.FullName -Raw
    
    # Check for net6.0-windows and replace with net8.0-windows
    if ($content -match '<TargetFramework>net6\.0-windows</TargetFramework>') {
        $newContent = $content -replace '<TargetFramework>net6\.0-windows</TargetFramework>', '<TargetFramework>net8\.0-windows</TargetFramework>'
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "  Upgraded to .NET 8"
    } 
    # Check for net6.0 and replace with net8.0 (for non-Windows specific projects)
    elseif ($content -match '<TargetFramework>net6\.0</TargetFramework>') {
        $newContent = $content -replace '<TargetFramework>net6\.0</TargetFramework>', '<TargetFramework>net8\.0</TargetFramework>'
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "  Upgraded to .NET 8"
    }
    else {
        Write-Host "  File does not contain TargetFramework version to upgrade"
    }
}

Write-Host ""
Write-Host "Upgrade completed! Please run dotnet restore to restore dependencies."