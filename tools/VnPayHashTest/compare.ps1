$urls = @(
    @{ label = 'Without-POS'; url = 'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=5600000&vnp_Command=pay&vnp_CreateDate=20260728083907&vnp_CurrCode=VND&vnp_ExpireDate=20260728084907&vnp_IpAddr=127.0.0.1&vnp_Locale=vn&vnp_OrderInfo=Thanh+toan+booking+BK20260728522540&vnp_OrderType=other&vnp_ReturnUrl=https%3A%2F%2Fflap-spotless-enclosure.ngrok-free.dev%2Fapi%2Fpayments%2Fvnpay%2Fpos-return&vnp_TmnCode=H45COC5D&vnp_TxnRef=05039d388e8e4394a1eafe354ff11d87&vnp_Version=2.1.0&vnp_SecureHash=327d3021798a09da2868e91e0a84c3ed77fa02b21dbbab2f99feda6491292511fdebab1a40e595f9130eb5f5ed85a4e625cdc0ee4dcca66d06f6135ba633dc77' },
    @{ label = 'With-POS';    url = 'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=5600000&vnp_Command=pay&vnp_CreateDate=20260728083907&vnp_CurrCode=VND&vnp_ExpireDate=20260728084907&vnp_IpAddr=127.0.0.1&vnp_Locale=vn&vnp_OrderInfo=Thanh+toan+booking+BK20260728522540+%28POS%29&vnp_OrderType=other&vnp_ReturnUrl=https%3A%2F%2Fflap-spotless-enclosure.ngrok-free.dev%2Fapi%2Fpayments%2Fvnpay%2Fpos-return&vnp_TmnCode=H45COC5D&vnp_TxnRef=d990b69f8acd4b8094966f7db1c84304&vnp_Version=2.1.0&vnp_SecureHash=4642826ac2c8a092142bcdce383eb36c799e09829ca3626c32290d78fa6d4c0f03a6602a9735e1362962dfdce356ec2b5d106b2e5d9107dbecd38c7868b1ed61' }
)
foreach ($u in $urls) {
    $out = "$env:TEMP\probe-$($u.label).html"
    curl -s -L $u.url -o $out
    $hit = Select-String -Path $out -Pattern 'Sai ch|errorCode|code=70|cardNumber' -CaseSensitive:$false | Select-Object -First 1
    Write-Host "[$($u.label)] " -NoNewline
    if ($hit -match 'Sai ch|code=70') { Write-Host "FAIL code=70" }
    elseif ($hit -match 'cardNumber') { Write-Host "OK - reached payment page" }
    else { Write-Host "?" }
    Write-Host "  $($hit.Line.Trim())"
}