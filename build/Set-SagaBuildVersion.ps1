param(
    [Parameter(Mandatory = $true)]
    [string]$StateFile
)

$ErrorActionPreference = 'Stop'

$today = [DateTime]::Now.ToString('yyyy.M.d', [Globalization.CultureInfo]::InvariantCulture)
$reuseWindowSeconds = 30
$stateDirectory = Split-Path -Parent $StateFile
if (-not [string]::IsNullOrWhiteSpace($stateDirectory)) {
    New-Item -ItemType Directory -Force -Path $stateDirectory | Out-Null
}

$mutex = [System.Threading.Mutex]::new($false, 'Global\SagaDailyBuildVersion')
$lockTaken = $false
try {
    try {
        $lockTaken = $mutex.WaitOne([TimeSpan]::FromSeconds(30))
    }
    catch [System.Threading.AbandonedMutexException] {
        $lockTaken = $true
    }

    if (-not $lockTaken) {
        throw 'Could not acquire the Saga build version lock.'
    }

    $previousDate = $null
    $previousSequence = -1
    if (Test-Path -LiteralPath $StateFile) {
        $state = Get-Content -LiteralPath $StateFile -Raw
        if ($state -match '^(?<date>\d{4}\.\d{1,2}\.\d{1,2})\.(?<sequence>\d+)$') {
            $previousDate = $Matches['date']
            $previousSequence = [int]$Matches['sequence']
        }

        $stateAge = (Get-Date) - (Get-Item -LiteralPath $StateFile).LastWriteTime
        if ($previousDate -eq $today -and $stateAge.TotalSeconds -lt $reuseWindowSeconds) {
            Write-Output $state.Trim()
            return
        }
    }

    $sequence = if ($previousDate -eq $today) { $previousSequence + 1 } else { 0 }
    $version = "$today.$sequence"
    Set-Content -LiteralPath $StateFile -Value $version -NoNewline
    Write-Output $version
}
finally {
    if ($lockTaken) {
        $mutex.ReleaseMutex()
    }

    $mutex.Dispose()
}
