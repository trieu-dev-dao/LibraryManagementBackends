$ErrorActionPreference = 'Stop'
$suffix = [guid]::NewGuid().ToString('N').Substring(0, 10)
$database = "LibraryReaderPortalTest_$suffix"
# shortcut: this check needs a local SQL Server with integrated authentication; use a disposable CI server when CI runs it.
$server = if ($env:READER_TEST_SQL_SERVER) { $env:READER_TEST_SQL_SERVER } else { 'DESKTOP-GAKAPEG\SQLEXPRESS' }
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$base = "http://127.0.0.1:$port"
$dll = Join-Path $PSScriptRoot '..\bin\Release\net10.0\LibraryManagement.dll'
$stdout = Join-Path $env:TEMP "reader-portal-$suffix.out.log"
$stderr = Join-Path $env:TEMP "reader-portal-$suffix.err.log"
$process = $null

function Assert($condition, $message) {
    if (-not $condition) { throw $message }
    Write-Output "PASS $message"
}

function Api($method, $path, $body = $null, $token = $null) {
    $args = @{ Method = $method; Uri = "$base$path" }
    if ($token) { $args.Headers = @{ Authorization = "Bearer $token" } }
    if ($null -ne $body) {
        $args.ContentType = 'application/json'
        $args.Body = $body | ConvertTo-Json -Compress -Depth 5
    }
    Invoke-RestMethod @args
}

function ExpectError($method, $path, $status, $body = $null, $token = $null) {
    try { $null = Api $method $path $body $token }
    catch [System.Net.WebException] {
        $response = $_.Exception.Response
        Assert ([int]$response.StatusCode -eq $status) "$method $path returns $status"
        $reader = [System.IO.StreamReader]::new($response.GetResponseStream())
        try { return $reader.ReadToEnd() | ConvertFrom-Json }
        finally { $reader.Dispose(); $response.Dispose() }
    }
    throw "Expected $status from $method $path"
}

function Sql($query) {
    $null = sqlcmd -S $server -E -C -b -d $database -Q $query
    if ($LASTEXITCODE -ne 0) { throw 'Test SQL command failed.' }
}

try {
    $env:ConnectionStrings__DefaultConnection = "Server=$server;Database=$database;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;Connect Timeout=30"
    $env:JwtSettings__SecretKey = [guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')
    $env:JwtSettings__Issuer = 'ReaderPortalTest'
    $env:JwtSettings__Audience = 'ReaderPortalTest'
    $env:JwtSettings__ExpirationInMinutes = '60'
    $env:ADMIN_EMAIL = "admin-$suffix@example.test"
    $env:ADMIN_PASSWORD = 'T3st!' + [guid]::NewGuid().ToString('N')
    $env:LIBRARIAN_EMAIL = "librarian-$suffix@example.test"
    $env:LIBRARIAN_PASSWORD = 'T3st!' + [guid]::NewGuid().ToString('N')
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:ASPNETCORE_URLS = $base
    $process = Start-Process -FilePath 'dotnet' -ArgumentList $dll -WorkingDirectory (Split-Path $dll) -RedirectStandardOutput $stdout -RedirectStandardError $stderr -WindowStyle Hidden -PassThru

    $ready = $false
    for ($attempt = 0; $attempt -lt 120; $attempt++) {
        if ($process.HasExited) { break }
        try { $null = Api 'Get' '/api/books'; $ready = $true; break }
        catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) {
        Get-Content $stderr, $stdout -ErrorAction SilentlyContinue
        throw 'Test API did not start.'
    }

    $admin = (Api 'Post' '/api/Auth/login' @{ username = $env:ADMIN_EMAIL; password = $env:ADMIN_PASSWORD }).token
    Assert (-not [string]::IsNullOrWhiteSpace($admin)) 'staff test account can sign in'
    $googleError = ExpectError 'Post' '/api/Auth/google-login' 401 @{ idToken = 'not-a-jwt' }
    Assert ($googleError.code -eq 'google_token_invalid' -and -not [string]::IsNullOrWhiteSpace($googleError.message)) 'invalid Google credential returns a JSON error'
    $readers = @()
    foreach ($n in 1..3) {
        $email = "reader-$n-$suffix@example.test"
        $password = 'T3st!' + [guid]::NewGuid().ToString('N')
        $null = Api 'Post' '/api/Auth/register' @{ fullName = "Reader $n"; username = "reader$n$suffix"; email = $email; password = $password }
        $readers += Api 'Post' '/api/Auth/login' @{ username = $email; password = $password }
    }
    $a = $readers[0]; $b = $readers[1]; $missing = $readers[2]
    Assert ($a.memberId -ne $b.memberId -and $a.userId -ne $b.userId) 'two readers have distinct account and member IDs'
    $empty = Api 'Get' '/api/reader/loans' $null $b.token
    Assert ($empty.activeLoanCount -eq 0 -and $empty.loans.Count -eq 0) 'linked reader has empty history'

    $author = Api 'Post' '/api/authors' @{ name = "Test Author $suffix" } $admin
    $category = Api 'Post' '/api/categories' @{ name = "Test Category $suffix" } $admin
    $book = Api 'Post' '/api/books' @{ title = "Test Book $suffix"; authorId = $author.id; categoryId = $category.id; quantity = 4; publishedYear = 2026 } $admin
    $today = (Get-Date).ToUniversalTime().AddHours(7).Date
    $due = $today.AddDays(1).ToString('yyyy-MM-dd')
    $aOpen = Api 'Post' '/api/borrow-records/borrow' @{ bookId = $book.id; memberId = $a.memberId; expiredDate = $due } $admin
    $aReturned = Api 'Post' '/api/borrow-records/borrow' @{ bookId = $book.id; memberId = $a.memberId; expiredDate = $due } $admin
    $bOpen = Api 'Post' '/api/borrow-records/borrow' @{ bookId = $book.id; memberId = $b.memberId; expiredDate = $due } $admin
    $bReturned = Api 'Post' '/api/borrow-records/borrow' @{ bookId = $book.id; memberId = $b.memberId; expiredDate = $due } $admin
    $null = Api 'Post' "/api/borrow-records/$($aReturned.id)/return" $null $admin
    $null = Api 'Post' "/api/borrow-records/$($bReturned.id)/return" $null $admin
    $yesterday = $today.AddDays(-1).ToString('yyyy-MM-dd')
    $lateUtc = $today.AddDays(3).AddHours(-7).ToString('yyyy-MM-ddTHH:mm:ss')
    Sql "UPDATE BorrowRecords SET ExpiredDate = '$yesterday' WHERE Id = $($aOpen.id); UPDATE BorrowRecords SET ReturnDate = '$lateUtc' WHERE Id = $($aReturned.id)"

    $aLoans = Api 'Get' '/api/reader/loans' $null $a.token
    $bLoans = Api 'Get' '/api/reader/loans' $null $b.token
    Assert ($aLoans.activeLoanCount -eq 1 -and $aLoans.loans.Count -eq 2 -and $aLoans.loans.id -notcontains $bOpen.id) 'reader A sees only own loans and full active count'
    Assert ($bLoans.activeLoanCount -eq 1 -and $bLoans.loans.Count -eq 2 -and $bLoans.loans.id -contains $bOpen.id -and $bLoans.loans.id -contains $bReturned.id) 'reader B sees only own loans'
    $selectorIgnored = Api 'Get' "/api/reader/loans?memberId=$($b.memberId)" $null $a.token
    Assert ($selectorIgnored.loans.id -notcontains $bOpen.id -and $selectorIgnored.loans.id -contains $aOpen.id) 'reader cannot select another member through a query parameter'
    $open = $aLoans.loans | Where-Object id -eq $aOpen.id
    $late = $aLoans.loans | Where-Object id -eq $aReturned.id
    $onTime = $bLoans.loans | Where-Object id -eq $bReturned.id
    $borrowing = [string][char]0x0110 + 'ang m' + [char]0x01B0 + [char]0x1EE3 + 'n'
    $overdue = 'Qu' + [char]0x00E1 + ' h' + [char]0x1EA1 + 'n'
    $returned = [string][char]0x0110 + [char]0x00E3 + ' tr' + [char]0x1EA3
    Assert ($open.status -eq $borrowing -and !$open.isCompleted -and $null -eq $open.returnDate -and $open.quantity -eq 1) 'past-due unreturned loan remains Borrowing'
    Assert ($late.status -eq $overdue -and $late.isCompleted -and $null -ne $late.returnDate) 'completed late return belongs to history'
    Assert ($onTime.status -eq $returned -and $onTime.isCompleted -and $null -ne $onTime.returnDate) 'completed on-time return belongs to history'
    $null = ExpectError 'Get' '/api/borrow-records' 403 $null $a.token
    $null = ExpectError 'Post' '/api/borrow-records/borrow' 403 @{ bookId = $book.id; memberId = $b.memberId; expiredDate = $due } $a.token

    $oldB = Api 'Get' '/api/reader/profile' $null $b.token
    $updated = Api 'Put' '/api/reader/profile' @{ fullName = 'Reader A Updated'; phone = '0901234567'; email = 'forbidden@example.test'; role = 'Admin'; userId = $b.userId; memberId = $b.memberId } $a.token
    Assert ($updated.fullName -eq 'Reader A Updated' -and $updated.phone -eq '0901234567' -and $updated.userId -eq $a.userId -and $updated.memberId -eq $a.memberId) 'reader A updates only own contact fields'
    Assert ($updated.email -eq "reader-1-$suffix@example.test" -and (Api 'Get' '/api/reader/profile' $null $b.token).fullName -eq $oldB.fullName) 'forbidden identity fields and reader B remain unchanged'
    $staffMember = Api 'Get' "/api/members/$($a.memberId)" $null $admin
    Assert ($staffMember.fullName -eq $updated.fullName -and $staffMember.phone -eq $updated.phone) 'linked member profile stays in sync'
    $cleared = Api 'Put' '/api/reader/profile' @{ fullName = 'Reader A Updated'; phone = $null } $a.token
    Assert ($null -eq $cleared.phone) 'null phone clears the optional contact field'
    $badName = ExpectError 'Put' '/api/reader/profile' 400 @{ fullName = '   '; phone = $null } $a.token
    Assert ($null -ne $badName.errors.FullName) 'blank name gives field-level error'
    $badPhone = ExpectError 'Put' '/api/reader/profile' 400 @{ fullName = 'Reader A'; phone = 'not a phone!' } $a.token
    Assert ($null -ne $badPhone.errors.Phone) 'invalid phone gives field-level error'

    Sql "DELETE FROM Members WHERE UserId = $($missing.userId)"
    $unlinked = Api 'Get' '/api/reader/profile' $null $missing.token
    Assert ($null -eq $unlinked.memberId) 'unlinked account profile reports null member ID'
    $errorBody = ExpectError 'Get' '/api/reader/loans' 409 $null $missing.token
    Assert ($errorBody.code -eq 'member_not_linked') 'missing member differs from empty history'
    $unlinkedUpdate = Api 'Put' '/api/reader/profile' @{ fullName = 'Unlinked Updated'; phone = $null } $missing.token
    Assert ($unlinkedUpdate.fullName -eq 'Unlinked Updated' -and $null -eq $unlinkedUpdate.memberId) 'unlinked account can update own allowed fields'
}
finally {
    if ($process -and !$process.HasExited) { Stop-Process -Id $process.Id -Force; $process.WaitForExit() }
    $drop = "IF DB_ID(N'$database') IS NOT NULL BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END"
    $null = sqlcmd -S $server -E -C -b -d master -Q $drop
    if ($LASTEXITCODE -ne 0) { Write-Warning "Could not drop disposable database $database" }
    Remove-Item -LiteralPath $stdout, $stderr -ErrorAction SilentlyContinue
}
