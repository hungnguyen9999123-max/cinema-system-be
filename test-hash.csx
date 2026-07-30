using System.Security.Cryptography;
using System.Text;

var secret = "7GWLDSS9B6NWSL55TAFJZ1T9ETZVS1E0";
var signData = "vnp_amount=7000000&vnp_bankcode=NCB&vnp_banktranno=VNP15615410&vnp_cardtype=ATM&vnp_orderinfo=Thanh+toan+booking+BK20260709671794&vnp_paydate=20260709152143&vnp_responsecode=00&vnp_tmncode=PRMFU27J&vnp_transactionno=15615410&vnp_transactionstatus=00&vnp_txnref=b7af8aa4eac24a9e8a910ddb3a59923e";
var receivedHash = "6680b1ee43a9be2f9c9009f632ab1baa26a7fb87eebf20b5eb40e6c11cc12612dd0cd7874952f09fcef0d619f45391fcd35c06c234a4f1bf304bf5d26515ac9f";

Console.WriteLine($"Secret: {secret}");
Console.WriteLine($"SignData: {signData}");
Console.WriteLine($"ReceivedHash: {receivedHash}");
Console.WriteLine();

byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
byte[] dataBytes = Encoding.UTF8.GetBytes(signData);

using var sha256 = new HMACSHA256(keyBytes);
var sha256Result = Convert.ToHexString(sha256.ComputeHash(dataBytes)).ToUpperInvariant();
Console.WriteLine($"SHA256: {sha256Result}");
Console.WriteLine($"SHA256 match: {sha256Result == receivedHash.ToUpperInvariant()}");
Console.WriteLine();

using var sha512 = new HMACSHA512(keyBytes);
var sha512Result = Convert.ToHexString(sha512.ComputeHash(dataBytes)).ToUpperInvariant();
Console.WriteLine($"SHA512: {sha512Result}");
Console.WriteLine($"SHA512 match: {sha512Result == receivedHash.ToUpperInvariant()}");
