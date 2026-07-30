using Microsoft.Data.SqlClient;
var cs = "Server=tcp:dbcinema.database.windows.net,1433;Database=cinema_db;User Id=cinemadb;Password=Aa@123456;Encrypt=true;TrustServerCertificate=False;Connection Timeout=15;";
var bookingId = Guid.Parse("c7515fe6-a60e-475c-826d-029f130a0439");
using var c = new SqlConnection(cs);
c.Open();
var q = new SqlCommand("SELECT id, status, amount, gateway, paid_at, created_at FROM PAYMENTS WHERE booking_id=@b", c);
q.Parameters.AddWithValue("@b", bookingId);
using var r = q.ExecuteReader();
Console.WriteLine($"{"Id",-40} {"Status",-12} {"Amount",-12} {"Gateway",-15} {"PaidAt",-30}");
int n = 0;
while (r.Read()) {
    n++;
    Console.WriteLine($"{r.GetGuid(0),-40} {r.GetString(1),-12} {r.GetDecimal(2),-12} {r.GetString(3),-15} {(r.IsDBNull(4)?"<null>":r.GetDateTime(4).ToString("O")),-30}");
}
r.Close();
Console.WriteLine($"\nTotal: {n} payment rows for booking {bookingId}");

// Also: what status values exist in PAYMENTS table?
var q2 = new SqlCommand("SELECT DISTINCT status FROM PAYMENTS", c);
using var r2 = q2.ExecuteReader();
Console.Write("Distinct PAYMENTS.status: ");
while (r2.Read()) Console.Write($"{r2.GetString(0)} ");
Console.WriteLine();
r2.Close();