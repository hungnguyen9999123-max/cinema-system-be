# Quick smoke test for Room / Seat / Showtime APIs
param(
    [string]$BaseUrl = "http://localhost:5293",
    [string]$TestEmail = "roomseat.test@cinema.local",
    [string]$TestPass = "Test@12345"
)

$ErrorActionPreference = "Stop"

function Write-Result($name, $ok, $detail = "") {
    if ($ok) {
        Write-Host "[PASS] $name $detail" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $name $detail" -ForegroundColor Red
    }
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers = @{},
        [object]$Body = $null
    )

    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $Headers
        ContentType = "application/json"
    }
    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10 -Compress)
    }
    try {
        return Invoke-RestMethod @params
    } catch {
        $detail = $_.Exception.Message
        if ($_.ErrorDetails.Message) { $detail = $_.ErrorDetails.Message }
        elseif ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $detail = $reader.ReadToEnd()
        }
        throw "$Method $Uri failed: $detail"
    }
}

Write-Host "=== Room / Seat / Showtime API Smoke Test ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"

# 1. Register (ignore if already exists)
try {
    $reg = Invoke-Api -Method POST -Uri "$BaseUrl/api/auth/register" -Body @{
        fullName = "Room Seat Tester"
        email = $TestEmail
        password = $TestPass
        confirmPassword = $TestPass
    }
    if (-not $reg.isSuccess) { throw $reg.message }
    $userId = $reg.data.userId
    $verifyToken = $reg.data.verificationToken
    Write-Result "Register" $true "- userId=$userId"
} catch {
    $existing = sqlcmd -S "(local)" -U sa -P "12345" -d cinema_db -Q "SET NOCOUNT ON; SELECT CAST(id AS varchar(36)) FROM USERS WHERE email='$TestEmail'" -h -1 -W 2>$null
    $userId = ($existing | Where-Object { $_ -match '^[0-9a-f-]{36}$' } | Select-Object -First 1)
    if (-not $userId) { throw "Register failed and no existing user: $($_.Exception.Message)" }
    Write-Result "Register (skip, user exists)" $true "- userId=$userId"
    $verifyToken = $null
}

# 2. Verify email if we have token
if ($verifyToken) {
    $verify = Invoke-Api -Method POST -Uri "$BaseUrl/api/auth/verify-email" -Body @{ token = $verifyToken }
    if (-not $verify.isSuccess) { throw "Verify failed: $($verify.message)" }
    Write-Result "Verify email" $true
} else {
    sqlcmd -S "(local)" -U sa -P "12345" -d cinema_db -Q "UPDATE USERS SET is_email_verified=1 WHERE email='$TestEmail'" | Out-Null
    Write-Result "Verify email (skip)" $true
}

# 3. Seed base data
$cinemaId = [guid]::NewGuid().ToString()
$seatTypeId = [guid]::NewGuid().ToString()
$movieId = [guid]::NewGuid().ToString()
$now = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")

sqlcmd -S "(local)" -U sa -P "12345" -d cinema_db -Q @"
UPDATE USERS SET role='Staff', is_email_verified=1 WHERE id='$userId';
IF NOT EXISTS (SELECT 1 FROM SEAT_TYPES WHERE name='Standard')
  INSERT INTO SEAT_TYPES (id,name,seat_multiplier,description,status)
  VALUES ('$seatTypeId','Standard',1.00,'Standard seat','ACTIVE');
INSERT INTO CINEMAS (id,name,address,city,phone,status,created_at,updated_at)
VALUES ('$cinemaId','Test Cinema $(Get-Date -Format 'HHmmss')','123 Test St','Hanoi','0900000000','ACTIVE','$now','$now');
INSERT INTO MOVIES (id,created_by,title,genre,language,duration_min,release_date,synopsis,age_rating,status,created_at,updated_at)
VALUES ('$movieId','$userId','Test Movie','Action','Vietnamese',120,'2026-01-01','Test synopsis','PG-13','NOW_SHOWING','$now','$now');
"@ | Out-Null
Write-Result "Seed DB" $true "- cinemaId=$cinemaId movieId=$movieId"

# 4. Login
$login = Invoke-Api -Method POST -Uri "$BaseUrl/api/auth/login" -Body @{
    email = $TestEmail
    password = $TestPass
}
if (-not $login.isSuccess) { throw "Login failed: $($login.message)" }
$headers = @{ Authorization = "Bearer $($login.data.accessToken)" }
Write-Result "Login as Staff" $true

# 5. GET rooms
$rooms = Invoke-Api -Method GET -Uri "$BaseUrl/api/rooms?page=1&pageSize=20" -Headers $headers
Write-Result "GET /api/rooms" $rooms.isSuccess "- total=$($rooms.data.totalCount)"

# 6. POST room
$room = Invoke-Api -Method POST -Uri "$BaseUrl/api/rooms" -Headers $headers -Body @{
    cinemaId = $cinemaId
    name = "Room A1-$(Get-Date -Format 'HHmmss')"
    roomType = "STANDARD"
    totalCapacity = 50
}
if (-not $room.isSuccess) { throw "Create room failed: $($room.message)" }
$roomId = $room.data.id
Write-Result "POST /api/rooms" $true "- roomId=$roomId"

# 7. GET room by id
$roomGet = Invoke-Api -Method GET -Uri "$BaseUrl/api/rooms/$roomId" -Headers $headers
Write-Result "GET /api/rooms/{id}" $roomGet.isSuccess "- name=$($roomGet.data.name)"

# 8. POST seat layout
$layout = Invoke-Api -Method POST -Uri "$BaseUrl/api/rooms/$roomId/seat-layout" -Headers $headers -Body @{
    rows = 3
    seatsPerRow = 5
    defaultSeatTypeName = "Standard"
    replaceExisting = $false
}
if (-not $layout.isSuccess) { throw "Generate layout failed: $($layout.message)" }
Write-Result "POST /api/rooms/{id}/seat-layout" $true "- totalSeats=$($layout.data.totalSeats)"

# 9. GET seat layout
$layoutGet = Invoke-Api -Method GET -Uri "$BaseUrl/api/rooms/$roomId/seat-layout" -Headers $headers
Write-Result "GET /api/rooms/{id}/seat-layout" $layoutGet.isSuccess "- rows=$($layoutGet.data.rows.Count)"

# 10. POST single seat
$seat = Invoke-Api -Method POST -Uri "$BaseUrl/api/rooms/$roomId/seats" -Headers $headers -Body @{
    rowLetter = "D"
    colNumber = 1
    seatTypeName = "Standard"
}
if (-not $seat.isSuccess) { throw "Create seat failed: $($seat.message)" }
$seatId = $seat.data.id
Write-Result "POST /api/rooms/{id}/seats" $true "- seatId=$seatId label=$($seat.data.seatLabel)"

# 11. PUT seat
$seatUpd = Invoke-Api -Method PUT -Uri "$BaseUrl/api/seats/$seatId" -Headers $headers -Body @{
    seatTypeName = "Standard"
    status = "ACTIVE"
}
Write-Result "PUT /api/seats/{id}" $seatUpd.isSuccess

# 12. POST showtime
$startTime = (Get-Date).AddDays(1).Date.AddHours(14)
$show = Invoke-Api -Method POST -Uri "$BaseUrl/api/showtimes" -Headers $headers -Body @{
    movieId = $movieId
    roomId = $roomId
    startTime = $startTime.ToString("yyyy-MM-ddTHH:mm:ss")
    timeSlot = "AFTERNOON"
    languageType = "SUBTITLED"
}
if (-not $show.isSuccess) { throw "Create showtime failed: $($show.message)" }
$showId = $show.data.id
Write-Result "POST /api/showtimes" $true "- showId=$showId endTime=$($show.data.endTime)"

# 13. GET showtimes
$shows = Invoke-Api -Method GET -Uri "$BaseUrl/api/showtimes?roomId=$roomId" -Headers $headers
Write-Result "GET /api/showtimes" $shows.isSuccess "- count=$($shows.data.items.Count)"

# 14. GET showtime by id
$showGet = Invoke-Api -Method GET -Uri "$BaseUrl/api/showtimes/$showId" -Headers $headers
Write-Result "GET /api/showtimes/{id}" $showGet.isSuccess "- movie=$($showGet.data.movieTitle)"

# 15. PUT showtime (partial update, no status field)
$showUpd = Invoke-Api -Method PUT -Uri "$BaseUrl/api/showtimes/$showId" -Headers $headers -Body @{
    timeSlot = "EVENING"
    languageType = "DUBBED"
}
Write-Result "PUT /api/showtimes/{id} (partial)" $showUpd.isSuccess "- timeSlot=$($showUpd.data.timeSlot)"

# 16. PUT room
$roomUpd = Invoke-Api -Method PUT -Uri "$BaseUrl/api/rooms/$roomId" -Headers $headers -Body @{
    name = "$($roomGet.data.name)-updated"
    roomType = "STANDARD"
    totalCapacity = 51
    status = "ACTIVE"
}
Write-Result "PUT /api/rooms/{id}" $roomUpd.isSuccess "- name=$($roomUpd.data.name)"

# 17. DELETE seat
$seatDel = Invoke-Api -Method DELETE -Uri "$BaseUrl/api/seats/$seatId" -Headers $headers
Write-Result "DELETE /api/seats/{id}" $seatDel.isSuccess

# 18. DELETE showtime (soft cancel if no bookings)
$showDel = Invoke-Api -Method DELETE -Uri "$BaseUrl/api/showtimes/$showId" -Headers $headers
Write-Result "DELETE /api/showtimes/{id}" $showDel.isSuccess

Write-Host ""
Write-Host "=== All smoke tests completed ===" -ForegroundColor Cyan
