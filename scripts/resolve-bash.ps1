# Resolve bash executable (Git for Windows preferred; skip broken WSL stubs).
param()

$gitBash = "C:\Program Files\Git\bin\bash.exe"
if (Test-Path $gitBash) {
    Write-Output $gitBash
    exit 0
}

$candidates = @("bash", "C:\Program Files\Git\usr\bin\bash.exe")
foreach ($candidate in $candidates) {
    if (-not (Get-Command $candidate -ErrorAction SilentlyContinue)) { continue }
    try {
        & $candidate -c "exit 0" 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Output $candidate
            exit 0
        }
    } catch { continue }
}

throw "bash required (install Git for Windows or add a working bash to PATH)"
