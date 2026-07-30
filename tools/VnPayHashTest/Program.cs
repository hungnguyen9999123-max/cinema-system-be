// Test VOI va KHONG co (POS) trong OrderInfo
using System.Security.Cryptography;
using System.Text;

string secret = "XD7Z982RIR3NDCVXIE3RHXP0R7QXS9BB";

void Test(string label, string orderInfo)
{
    var p = new SortedDictionary<string, string>(StringComparer.Ordinal)
    {
        ["vnp_Version"] = "2.1.0",
        ["vnp_Command"] = "pay",
        ["vnp_TmnCode"] = "H45COC5D",
        ["vnp_Amount"] = "5600000",
        ["vnp_CreateDate"] = "20260728083907",
        ["vnp_CurrCode"] = "VND",
        ["vnp_ExpireDate"] = "20260728084907",
        ["vnp_IpAddr"] = "127.0.0.1",
        ["vnp_Locale"] = "vn",
        ["vnp_OrderInfo"] = orderInfo,
        ["vnp_OrderType"] = "other",
        ["vnp_ReturnUrl"] = "https://flap-spotless-enclosure.ngrok-free.dev/api/payments/vnpay/pos-return",
        ["vnp_TxnRef"] = Guid.NewGuid().ToString("N")
    };

    string signData = string.Join("&", p
        .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value).Replace("%20", "+")}"));
    using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
    string hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signData))).ToLowerInvariant();

    string url = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?{signData}&vnp_SecureHash={hash}";
    Console.WriteLine($"=== {label} ===");
    Console.WriteLine(url);
    Console.WriteLine();
}

Test("Without (POS)", "Thanh toan booking BK20260728522540");
Test("With (POS)", "Thanh toan booking BK20260728522540 (POS)");
Test("Vietnamese POS", "Thanh toan booking BK20260728522540 POS");
Test("VNPay POS keyword", "POS Thanh toan booking BK20260728522540");