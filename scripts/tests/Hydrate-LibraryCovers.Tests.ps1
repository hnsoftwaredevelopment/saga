$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'Hydrate-LibraryCovers.ps1'

Describe 'Hydrate-LibraryCovers' {
    BeforeEach {
        $testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
        $bookRoot = Join-Path $testRoot 'books\aa\book-id'
        New-Item -ItemType Directory -Path $bookRoot -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $testRoot 'library.db') | Out-Null
        [System.IO.File]::WriteAllBytes((Join-Path $bookRoot 'cover.jpg'), [byte[]](1, 2, 3, 4))
        [System.IO.File]::WriteAllBytes((Join-Path $bookRoot 'book.epub'), [byte[]](5, 6, 7, 8))
        [System.IO.File]::WriteAllBytes((Join-Path $bookRoot 'other.jpg'), [byte[]](9, 10))
        $logPath = Join-Path $testRoot 'hydration.log'
    }

    AfterEach {
        Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'refuses a directory that is not a Saga library' {
        Remove-Item -LiteralPath (Join-Path $testRoot 'library.db') -Force

        { & $scriptPath -LibraryPath $testRoot -ScanOnly -LogPath $logPath } | Should Throw
    }

    It 'scan mode finds only covers and leaves local files unread' {
        $result = & $scriptPath -LibraryPath $testRoot -ScanOnly -LogPath $logPath

        $result.TotalCovers | Should Be 1
        $result.LocalCovers | Should Be 1
        $result.PendingCovers | Should Be 0
        $result.HydratedCovers | Should Be 0
        $result.FailedCovers | Should Be 0
        (Test-Path -LiteralPath $logPath) | Should Be $true
    }

    It 'can read a local test cover without touching the ebook' {
        $ebookPath = Join-Path $bookRoot 'book.epub'
        $ebookHashBefore = (Get-FileHash -LiteralPath $ebookPath -Algorithm SHA256).Hash

        $result = & $scriptPath -LibraryPath $testRoot -IncludeLocalFiles -LogPath $logPath

        $result.TotalCovers | Should Be 1
        $result.HydratedCovers | Should Be 1
        $result.FailedCovers | Should Be 0
        (Get-FileHash -LiteralPath $ebookPath -Algorithm SHA256).Hash | Should Be $ebookHashBefore
    }
}
