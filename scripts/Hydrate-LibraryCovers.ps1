[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $LibraryPath,

    [switch] $ScanOnly,

    [switch] $IncludeLocalFiles,

    [string] $LogPath,

    [ValidateRange(1, 100000)]
    [int] $ProgressInterval = 100
)

$ErrorActionPreference = 'Stop'
$offlineAttribute = 0x00001000
$recallOnOpenAttribute = 0x00040000
$recallOnDataAccessAttribute = 0x00400000

function Test-CoverNeedsHydration {
    param([System.IO.FileInfo] $File)

    $attributes = [int64]$File.Attributes
    return (($attributes -band $offlineAttribute) -ne 0) -or
        (($attributes -band $recallOnOpenAttribute) -ne 0) -or
        (($attributes -band $recallOnDataAccessAttribute) -ne 0)
}

function Write-HydrationLog {
    param([string] $Message)

    $timestamp = [DateTimeOffset]::Now.ToString('yyyy-MM-dd HH:mm:ss zzz')
    Add-Content -LiteralPath $LogPath -Value "$timestamp $Message" -Encoding UTF8
}

$resolvedLibraryPath = [System.IO.Path]::GetFullPath(
    (Resolve-Path -LiteralPath $LibraryPath -ErrorAction Stop).Path)
$databasePath = Join-Path $resolvedLibraryPath 'library.db'
$booksPath = Join-Path $resolvedLibraryPath 'books'
if (-not (Test-Path -LiteralPath $databasePath -PathType Leaf)) {
    throw "De gekozen map bevat geen Saga library.db: $resolvedLibraryPath"
}

if (-not (Test-Path -LiteralPath $booksPath -PathType Container)) {
    throw "De gekozen Saga-bibliotheek bevat geen books-map: $resolvedLibraryPath"
}

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = Join-Path $env:LOCALAPPDATA 'Saga\maintenance\cover-hydration.log'
}

$logDirectory = Split-Path $LogPath -Parent
if (-not [string]::IsNullOrWhiteSpace($logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

$coverFiles = @(Get-ChildItem -LiteralPath $booksPath -Recurse -File -Filter 'cover.jpg' -ErrorAction Stop)
$pendingFiles = @($coverFiles | Where-Object { Test-CoverNeedsHydration $_ })
$localCoverCount = $coverFiles.Count - $pendingFiles.Count
$filesToRead = if ($IncludeLocalFiles) { $coverFiles } else { $pendingFiles }
$hydratedCount = 0
$failedCount = 0
$processedCount = 0

Write-HydrationLog "START library='$resolvedLibraryPath' total=$($coverFiles.Count) local=$localCoverCount pending=$($pendingFiles.Count) scanOnly=$ScanOnly"

if (-not $ScanOnly) {
    $buffer = New-Object byte[] (1024 * 1024)
    foreach ($cover in $filesToRead) {
        $processedCount++
        $percent = if ($filesToRead.Count -eq 0) { 100 } else {
            [Math]::Min(100, [Math]::Floor(($processedCount * 100.0) / $filesToRead.Count))
        }
        Write-Progress -Activity 'Saga-omslagen lokaal beschikbaar maken' `
            -Status "$processedCount van $($filesToRead.Count)" `
            -PercentComplete $percent

        try {
            $stream = [System.IO.FileStream]::new(
                $cover.FullName,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::ReadWrite,
                $buffer.Length,
                [System.IO.FileOptions]::SequentialScan)
            try {
                while ($stream.Read($buffer, 0, $buffer.Length) -gt 0) {
                }
            }
            finally {
                $stream.Dispose()
            }

            $hydratedCount++
        }
        catch {
            $failedCount++
            Write-HydrationLog "ERROR path='$($cover.FullName)' message='$($_.Exception.Message)'"
        }

        if (($processedCount % $ProgressInterval) -eq 0) {
            Write-HydrationLog "PROGRESS processed=$processedCount hydrated=$hydratedCount failed=$failedCount remaining=$($filesToRead.Count - $processedCount)"
        }
    }

    Write-Progress -Activity 'Saga-omslagen lokaal beschikbaar maken' -Completed
}

Write-HydrationLog "DONE processed=$processedCount hydrated=$hydratedCount failed=$failedCount"

[pscustomobject]@{
    LibraryPath = $resolvedLibraryPath
    LogPath = $LogPath
    TotalCovers = $coverFiles.Count
    LocalCovers = $localCoverCount
    PendingCovers = $pendingFiles.Count
    HydratedCovers = $hydratedCount
    FailedCovers = $failedCount
    ScanOnly = [bool]$ScanOnly
}
