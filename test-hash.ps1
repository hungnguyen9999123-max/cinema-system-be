$secret = "7GWLDSS9B6NWSL55TAFJZ1T9ETZVS1E0"
$signData = "vnp_amount=7000000&vnp_bankcode=NCB&vnp_banktranno=VNP15615410&vnp_cardtype=ATM&vnp_orderinfo=Thanh+toan+booking+BK20260709671794&vnp_paydate=20260709152143&vnp_responsecode=00&vnp_tmncode=PRMFU27J&vnp_transactionno=15615410&vnp_transactionstatus=00&vnp_txnref=b7af8aa4eac24a9e8a910ddb3a59923e"
$receivedHash = "6680b1ee43a9be2f9c9009f632ab1baa26a7fb87eebf20b5eb40e6c11cc12612dd0cd7874952f09fcef0d619f45391fcd35c06c234a4f1bf304bf5d26515ac9f"

Write-Host "Secret: $secret"
Write-Host "SignData: $signData"
Write-Host "ReceivedHash: $receivedHash"
Write-Host ""

$keyBytes = [System.Text.Encoding]::UTF8.GetBytes($secret)
$dataBytes = [System.Text.Encoding]::UTF8.GetBytes($signData)

$sha256 = [System.Security.Cryptography.HMACSHA256]::new($keyBytes)
$sha256Hash = ([BitConverter]::ToString($sha256.ComputeHash($dataBytes)) -replace '-', '').ToUpper()
Write-Host "SHA256: $sha256Hash"
Write-Host "SHA256 match: $($sha256Hash -eq $receivedHash.ToUpper())"
Write-Host ""

$sha512 = [System.Security.Cryptography.HMACSHA512]::new($keyBytes)
$sha512Hash = ([BitConverter]::ToString($sha512.ComputeHash($dataBytes)) -replace '-', '').ToUpper()
Write-Host "SHA512: $sha512Hash"
Write-Host "SHA512 match: $($sha512Hash -eq $receivedHash.ToUpper())"
