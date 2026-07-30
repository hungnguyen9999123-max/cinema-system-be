$cs = "Server=tcp:dbcinema.database.windows.net,1433;Database=cinema_db;User Id=cinemadb;Password=Aa@123456;Encrypt=true;TrustServerCertificate=False;Connection Timeout=15;"
Add-Type -AssemblyName "Microsoft.Data.SqlClient"
$sb = [Microsoft.Data.SqlClient.SqlConnection]::new($cs)
$sb.Open()
$q = @"
SELECT TOP 5
  b.id AS booking_id,
  b.customer_id,
  b.status,
  (SELECT COUNT(*) FROM TICKETS t WHERE t.booking_id = b.id) AS tickets,
  (SELECT COUNT(*) FROM FNB_ORDERS fo WHERE fo.booking_id = b.id) AS fnb_orders,
  (SELECT COUNT(*) FROM BOOKING_SEATS bs WHERE bs.booking_id = b.id) AS seats,
  (SELECT COUNT(*) FROM USERS u WHERE u.id = b.customer_id AND u.role='STAFF') AS is_staff
FROM BOOKINGS b
WHERE b.status IN ('CONFIRMED','COMPLETED')
ORDER BY b.created_at DESC
"@
$da = New-Object Microsoft.Data.SqlClient.SqlDataAdapter($q, $sb)
$tbl = New-Object System.Data.DataTable
$da.Fill($tbl) | Out-Null
$tbl | Format-Table -AutoSize

Write-Host "`nSample STAFF/ADMIN/MANAGER users:"
$uq = "SELECT TOP 5 id, email, full_name, role FROM USERS WHERE role IN ('STAFF','ADMIN','MANAGER') ORDER BY role, full_name"
$da2 = New-Object Microsoft.Data.SqlClient.SqlDataAdapter($uq, $sb)
$tbl2 = New-Object System.Data.DataTable
$da2.Fill($tbl2) | Out-Null
$tbl2 | Format-Table -AutoSize

Write-Host "`nSample CUSTOMER users:"
$cq = "SELECT TOP 5 id, email, full_name, role FROM USERS WHERE role='CUSTOMER' ORDER BY created_at DESC"
$da3 = New-Object Microsoft.Data.SqlClient.SqlDataAdapter($cq, $sb)
$tbl3 = New-Object System.Data.DataTable
$da3.Fill($tbl3) | Out-Null
$tbl3 | Format-Table -AutoSize
$sb.Close()