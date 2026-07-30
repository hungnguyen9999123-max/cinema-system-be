using BCrypt.Net;

const string pw = "Test@123456";
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(pw, workFactor: 11));