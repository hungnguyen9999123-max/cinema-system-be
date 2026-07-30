using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

const string connectionString = "Server=(local);database=cinema_db;uid=sa;pwd=12345;TrustServerCertificate=True;";
const int targetCount = 1000;
const string emailPrefix = "testuser";
const string emailDomain = "cinema.test";
const string defaultPassword = "Test@123456";
var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

var options = new DbContextOptionsBuilder<CinemaDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new CinemaDbContext(options);
var userEntity = db.Model.FindEntityType(typeof(User))
    ?? throw new InvalidOperationException("Cannot find User entity in CinemaDbContext.");

var dbSet = db.Set<User>();
var existingEmails = await dbSet
    .Select(u => EF.Property<string>(u, "Email"))
    .Where(email => email.StartsWith(emailPrefix) && email.EndsWith("@" + emailDomain))
    .ToListAsync();

var existingIndexes = existingEmails
    .Select(ParseSeedIndex)
    .Where(index => index.HasValue)
    .Select(index => index!.Value)
    .ToHashSet();

var users = new List<User>(targetCount);
for (var i = 1; i <= targetCount; i++)
{
    if (existingIndexes.Contains(i))
    {
        continue;
    }

    var user = new User();
    FillUser(user, userEntity, i, defaultPasswordHash);
    users.Add(user);
}

await dbSet.AddRangeAsync(users);
await db.SaveChangesAsync();

Console.WriteLine($"Inserted {users.Count} test users into cinema_db.");
Console.WriteLine($"Email range: {emailPrefix}0001@{emailDomain} ... {emailPrefix}{targetCount:0000}@{emailDomain}");
Console.WriteLine($"Password for all seeded users: {defaultPassword}");

static int? ParseSeedIndex(string email)
{
    if (!email.StartsWith(emailPrefix) || !email.EndsWith("@" + emailDomain))
    {
        return null;
    }

    var number = email[emailPrefix.Length..email.IndexOf('@')];
    return int.TryParse(number, out var index) ? index : null;
}

static void FillUser(User user, IEntityType userEntity, int index, string defaultPasswordHash)
{
    var email = $"{emailPrefix}{index:0000}@{emailDomain}";
    var now = DateTime.UtcNow;

    foreach (var property in userEntity.GetProperties())
    {
        if (property.IsPrimaryKey() && property.ValueGenerated != ValueGenerated.Never)
        {
            continue;
        }

        if (property.PropertyInfo is null)
        {
            continue;
        }

        var value = GetValueForProperty(property, index, email, now, defaultPasswordHash);
        if (value is not null)
        {
            value = ConvertToType(value, Nullable.GetUnderlyingType(property.PropertyInfo.PropertyType) ?? property.PropertyInfo.PropertyType);
        }

        if (value is not null || IsNullable(property))
        {
            property.PropertyInfo.SetValue(user, value);
        }
    }
}

static object? GetValueForProperty(IProperty property, int index, string email, DateTime now, string defaultPasswordHash)
{
    var name = property.Name;
    var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

    if (name.Contains("Email", StringComparison.OrdinalIgnoreCase))
    {
        return email;
    }

    if (name.Contains("Password", StringComparison.OrdinalIgnoreCase))
    {
        return defaultPasswordHash;
    }

    if (name.Contains("Birth", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Dob", StringComparison.OrdinalIgnoreCase))
    {
        return ConvertToType(new DateTime(1995, 1, 1).AddDays(index % 3650), clrType);
    }

    if (clrType == typeof(DateTime))
    {
        return now;
    }

    if (clrType == typeof(DateOnly))
    {
        return DateOnly.FromDateTime(now);
    }

    if (name.Equals("Role", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith("Role", StringComparison.OrdinalIgnoreCase))
    {
        return ConvertToType("Customer", clrType);
    }

    if (name.Contains("FullName", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Name", StringComparison.OrdinalIgnoreCase))
    {
        return $"Test User {index:0000}";
    }

    if (name.Contains("UserName", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Username", StringComparison.OrdinalIgnoreCase))
    {
        return $"testuser{index:0000}";
    }

    if (name.Contains("FirstName", StringComparison.OrdinalIgnoreCase))
    {
        return "Test";
    }

    if (name.Contains("LastName", StringComparison.OrdinalIgnoreCase))
    {
        return $"User {index:0000}";
    }

    if (name.Contains("Phone", StringComparison.OrdinalIgnoreCase))
    {
        return $"090{index:0000000}";
    }

    if (name.Contains("Address", StringComparison.OrdinalIgnoreCase))
    {
        return $"Seed address {index:0000}";
    }

    if (name.Contains("Gender", StringComparison.OrdinalIgnoreCase))
    {
        return ConvertToType(index % 2 == 0 ? "Male" : "Female", clrType);
    }

    if ((name.Contains("Verified", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Active", StringComparison.OrdinalIgnoreCase)) && clrType == typeof(bool))
    {
        return ConvertToType(true, clrType);
    }

    if (name.Contains("Deleted", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Locked", StringComparison.OrdinalIgnoreCase))
    {
        return ConvertToType(false, clrType);
    }

    if (name.Contains("Status", StringComparison.OrdinalIgnoreCase))
    {
        return ConvertToType("Active", clrType);
    }

    if (property.IsNullable)
    {
        return null;
    }

    if (clrType == typeof(string))
    {
        return $"{name}-{index:0000}";
    }

    if (clrType == typeof(bool))
    {
        return false;
    }

    if (clrType == typeof(int))
    {
        return 0;
    }

    if (clrType == typeof(long))
    {
        return 0L;
    }

    if (clrType == typeof(decimal))
    {
        return 0m;
    }

    if (clrType == typeof(Guid))
    {
        return Guid.NewGuid();
    }

    return null;
}

static bool IsNullable(IProperty property)
{
    return property.IsNullable || Nullable.GetUnderlyingType(property.ClrType) is not null;
}

static object ConvertToType(object value, Type targetType)
{
    if (targetType == typeof(bool) && value is string stringValue)
    {
        return stringValue.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("Customer", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("Male", StringComparison.OrdinalIgnoreCase);
    }

    if (targetType.IsEnum)
    {
        return Enum.Parse(targetType, value.ToString()!, ignoreCase: true);
    }

    return Convert.ChangeType(value, targetType);
}
