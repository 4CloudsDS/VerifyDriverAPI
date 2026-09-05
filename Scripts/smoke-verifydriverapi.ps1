param(
    [string]$ApiProject = "VerifyDriversAPI.csproj",
    [string]$FrontendUrl = "https://localhost:7172",
    [string]$ApiUrl = "http://localhost:5088"
)

$ErrorActionPreference = "Stop"

$api = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", $ApiProject, "--urls", $ApiUrl) -PassThru -WindowStyle Hidden
try {
    Start-Sleep -Seconds 8

    Invoke-RestMethod "$ApiUrl/api/Health" | Out-Null
    Invoke-RestMethod "$ApiUrl/api/Users" | Out-Null
    Invoke-RestMethod "$ApiUrl/api/Profiles/search?query=Fleet&mode=opportunity&relationshipType=Fleet%20contract" | Out-Null

    try {
        Invoke-WebRequest "$FrontendUrl" -UseBasicParsing | Out-Null
        Write-Host "Frontend responded at $FrontendUrl"
    }
    catch {
        Write-Warning "Frontend check skipped or unavailable at ${FrontendUrl}: $($_.Exception.Message)"
    }

    Write-Host "VerifyDriverAPI smoke checks passed."
}
finally {
    if (!$api.HasExited) {
        Stop-Process -Id $api.Id
    }
}
