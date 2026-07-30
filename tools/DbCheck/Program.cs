using Microsoft.Data.SqlClient;

var cs = "Server=tcp:dbcinema.database.windows.net,1433;Database=cinema_db;User Id=cinemadb;Password=Aa@123456;Encrypt=true;TrustServerCertificate=False;Connection Timeout=15;";
var bookingId = Guid.Parse("1d777b1f-b031-4d3a-a7e0-f62fabec9b7c");
var customerId = Guid.Parse("e0dc5fc7-e867-4ae9-ba91-ea3cf96b9102");
var itemId = Guid.Parse("c6652a0f-668e-412a-9020-288daa24531d");

using var c = new SqlConnection(cs);
c.Open();

// 1) Verify booking is CONFIRMED
var chk = new SqlCommand("SELECT status FROM BOOKINGS WHERE id=@b", c);
chk.Parameters.AddWithValue("@b", bookingId);
var status = (string?)chk.ExecuteScalar();
Console.WriteLine($"Booking status: {status}");
if (status != "CONFIRMED") { Console.WriteLine("Not confirmed, abort"); return; }

// 2) Create FNB order
var orderId = Guid.NewGuid();
var ins = new SqlCommand(@"
INSERT INTO FNB_ORDERS (id, booking_id, customer_id, order_status, total_amount, payment_method)
OUTPUT INSERTED.id
VALUES (@id, @bk, @cu, 'PENDING', @amt, 'CASH')", c);
ins.Parameters.AddWithValue("@id", orderId);
ins.Parameters.AddWithValue("@bk", bookingId);
ins.Parameters.AddWithValue("@cu", customerId);
ins.Parameters.AddWithValue("@amt", 14066666.00m);
var newId = (Guid)ins.ExecuteScalar()!;
Console.WriteLine($"FnbOrderId={newId}");

// 3) Add 2 details (qty 2)
var d1 = new SqlCommand(@"
INSERT INTO FNB_ORDER_DETAILS (fnb_order_id, item_id, quantity, unit_price, subtotal)
VALUES (@o, @i, 2, @p, @s)", c);
d1.Parameters.AddWithValue("@o", newId);
d1.Parameters.AddWithValue("@i", itemId);
d1.Parameters.AddWithValue("@p", 7033333.00m);
d1.Parameters.AddWithValue("@s", 14066666.00m);
d1.ExecuteNonQuery();
Console.WriteLine("Fnb detail inserted (qty=2)");

// 4) Also drop any existing QR tickets so we generate fresh
var del = new SqlCommand("DELETE FROM QrTickets WHERE booking_id=@b", c);
del.Parameters.AddWithValue("@b", bookingId);
var d = del.ExecuteNonQuery();
Console.WriteLine($"Old QR tickets cleared: {d}");