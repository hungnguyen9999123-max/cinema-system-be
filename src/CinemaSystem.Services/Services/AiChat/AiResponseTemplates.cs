namespace CinemaSystem.Services.Services.AiChat;

public static class AiResponseTemplates
{
    public static string GetGreetingResponse(string? userName = null)
    {
        var greeting = userName != null 
            ? "Xin chao " + userName + "! 👋" 
            : "Xin chao ban! 👋";
            
        var result = greeting + @"

Minh la tro ly ao cua rap chieu phim. Minh co the giup ban:

🎬 Tim phim dang chieu
📅 Xem lich chieu
🎟️ Huong dan dat ve
💰 Gia ve cac loai
🍿 Do an & nuoc uong

Ban can ho tro gi nao?";
        return result;
    }

    public static string GetFarewellResponse()
    {
        return "Cam on ban da tro chuyen! Neu can ho tro gi them, hay quay lai nhe. Chuc ban xem phim vui ve! 🎬✨";
    }

    public static string GetHumanHandoffResponse()
    {
        return @"Duoc roi, minh se chuyen ban den nhan vien ho tro.

Vui long cho trong giay lat, nhan vien se phan hoi som nhat co the. 🙏";
    }

    public static string GetMovieListResponse(List<MovieSuggestionDto> movies)
    {
        if (movies == null || movies.Count == 0)
            return "Hien tai khong co phim nao dang chieu. Ban co the xem phim sap ra mat nhe!";

        var movieList = string.Join("\n\n", movies.Select((m, i) => 
            "🎬 **" + m.Title + "**\n   📽️ The loai: " + m.Genre + "\n   ⏱️ Thoi luong: " + m.DurationMin + " phut\n   📌 Trang thai: " + GetStatusText(m.Status)
        ));

        return "Duoi day la cac phim dang chieu hot nhat:\n\n" + movieList + "\n\nBan muon xem chi tiet phim nao? Minh co the cho ban biet them ve lich chieu va gia ve nhe!";
    }

    public static string GetSearchMovieResponse(string query, List<MovieSuggestionDto> movies)
    {
        if (movies == null || movies.Count == 0)
            return "Minh khong tim thay phim nao lien quan den \"" + query + "\". Ban thu tim tu khoa khac xem sao?";

        var movieList = string.Join("\n\n", movies.Select(m => 
            "🎬 **" + m.Title + "**\n   📽️ The loai: " + m.Genre + "\n   ⏱️ " + m.DurationMin + " phut"
        ));

        return "Ket qua tim kiem cho \"" + query + "\":\n\n" + movieList + "\n\nBan muon biet them thong tin gi ve cac phim nay?";
    }

    public static string GetShowtimeResponse(List<ShowtimeSuggestionDto> showtimes)
    {
        if (showtimes == null || showtimes.Count == 0)
            return "Hien tai khong co suat chieu nao cho phim nay trong thoi gian toi.";

        var grouped = showtimes
            .GroupBy(s => s.ShowDateTime.ToString("dd/MM/yyyy"))
            .Take(3);

        var result = "📅 **Lich chieu sap toi:**\n\n";
        
        foreach (var day in grouped)
        {
            result += "📆 **" + day.Key + "**\n";
            foreach (var show in day.Take(3))
            {
                result += "   🕐 " + show.ShowDateTime.ToString("HH:mm") + " - " + show.RoomName + "\n";
            }
            result += "\n";
        }

        result += "Ban muon dat ve suat chieu nao?";

        return result;
    }

    public static string GetBookingHelpResponse()
    {
        return @"🎟️ **Huong dan dat ve:**

1️⃣ Chon phim ban muon xem
2️⃣ Chon ngay va suat chieu phu hop
3️⃣ Chon ghe ngoi (ghe trong mau xanh la)
4️⃣ Them do an & nuoc uong (neu can)
5️⃣ Tien hanh thanh toan

Mat khoang 2-3 phut de hoan tat dat ve!

Ban muon minh giup tim phim khong?";
    }

    public static string GetPriceResponse(List<FnbItemDto>? fnbItems = null)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("💰 **Bang gia ve tham khao:**");
        sb.AppendLine();
        sb.AppendLine("🎫 **Ve thuong:** 45.000 - 65.000 VND");
        sb.AppendLine("   (Tuy theo ghe va khung gio)");
        sb.AppendLine();
        sb.AppendLine("🎫 **Ve VIP:** 85.000 - 120.000 VND");
        sb.AppendLine("   (Ghe rong, co cho de chan rong)");
        sb.AppendLine();
        sb.AppendLine("👨‍👩‍👧 **Ve tre em:** Giam 20%");
        sb.AppendLine("   (Duoi 1.3m hoac duoi 12 tuoi)");
        
        if (fnbItems != null && fnbItems.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🍿 **Gia F&B hien tai:**");
            
            var drinks = fnbItems.Where(i => i.Category == "DRINK").ToList();
            var foods = fnbItems.Where(i => i.Category == "FOOD").ToList();
            var combos = fnbItems.Where(i => i.Category == "COMBO").ToList();
            
            if (drinks.Count > 0)
            {
                sb.AppendLine("🥤 **Nuoc uong:**");
                foreach (var item in drinks.Take(5))
                {
                    sb.AppendLine("   - " + item.Name + ": " + item.Price.ToString("N0") + " VND");
                }
            }
            
            if (foods.Count > 0)
            {
                sb.AppendLine("🍕 **Do an:**");
                foreach (var item in foods.Take(5))
                {
                    sb.AppendLine("   - " + item.Name + ": " + item.Price.ToString("N0") + " VND");
                }
            }
            
            if (combos.Count > 0)
            {
                sb.AppendLine("✨ **Combo:**");
                foreach (var item in combos.Take(3))
                {
                    sb.AppendLine("   - " + item.Name + ": " + item.Price.ToString("N0") + " VND");
                }
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("🍿 **Combo F&B:**");
            sb.AppendLine("   - Bap + Nuoc nho: 45.000 VND");
            sb.AppendLine("   - Bap + Nuoc lon: 65.000 VND");
            sb.AppendLine("   - Full combo: 89.000 VND");
        }
        
        sb.AppendLine();
        sb.AppendLine("Gia co the thay doi theo tung phim va khung gio. Ban muon dat ve khong?");

        return sb.ToString();
    }

    public static string GetFnbResponse(List<FnbItemDto>? fnbItems = null)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("🍿 **Menu Do an & Nuoc uong:**");
        sb.AppendLine();

        if (fnbItems != null && fnbItems.Count > 0)
        {
            var grouped = fnbItems.GroupBy(i => GetCategoryName(i.Category));
            
            foreach (var group in grouped)
            {
                sb.AppendLine("**" + group.Key + ":**");
                foreach (var item in group)
                {
                    var emoji = GetCategoryEmoji(item.Category);
                    sb.AppendLine(emoji + " " + item.Name + ": " + item.Price.ToString("N0") + " VND");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("🍿 **Bap rang bo:**");
            sb.AppendLine("   - Nho: 25.000 VND");
            sb.AppendLine("   - Vua: 35.000 VND");
            sb.AppendLine("   - Lon: 45.000 VND");
            sb.AppendLine();
            sb.AppendLine("🥤 **Nuoc uong:**");
            sb.AppendLine("   - Nuoc suoi: 15.000 VND");
            sb.AppendLine("   - Coca/Pepsi/Sprite: 20.000 VND");
            sb.AppendLine("   - Nuoc cam/nuoc ep: 30.000 VND");
            sb.AppendLine("   - Tra sua: 35.000 VND");
            sb.AppendLine();
            sb.AppendLine("🍕 **Do an khac:**");
            sb.AppendLine("   - Hotdog: 35.000 VND");
            sb.AppendLine("   - Khoai tay chien: 30.000 VND");
            sb.AppendLine("   - Ga vien: 40.000 VND");
            sb.AppendLine();
            sb.AppendLine("✨ **Combo tiet kiem:**");
            sb.AppendLine("   - Combo nho (bap vua + nuoc nho): 45.000 VND");
            sb.AppendLine("   - Combo lon (bap lon + nuoc lon): 65.000 VND");
        }

        sb.AppendLine("Ban muon dat gi nao?");

        return sb.ToString();
    }

    public static string GetFallbackResponse(List<MovieSuggestionDto>? suggestions = null)
    {
        if (suggestions != null && suggestions.Count > 0)
        {
            var movieNames = string.Join(", ", suggestions.Take(3).Select(m => "**" + m.Title + "**"));
            
            return "Hmm, minh chua hieu y ban lam 😅\n\nNhung ma... ban da xem nhung bo phim hot nay chua?\n" + movieNames + "\n\nHay ban thu hoi theo cac cach sau:\n- \"Phim nao dang chieu?\"\n- \"Huong dan dat ve\"\n- \"Gia ve bao nhieu?\"\n\nMinh san sang giup ban! 🎬";
        }

        return @"Minh chua hieu y ban lam 😅

Ban co the hoi minh theo cac cach sau:
- Phim nao dang chieu?
- Tim phim [ten phim]
- Lich chieu phim [ten]
- Huong dan dat ve
- Gia ve bao nhieu?

Minh san sang giup ban! 🎬";
    }

    public static string GetUnknownIntentResponse()
    {
        return @"Minh chua hieu y ban lam 😅

Ban co the hoi minh ve:
🎬 Phim dang chieu
📅 Lich chieu
🎟️ Cach dat ve
💰 Gia ve
🍿 Do an & nuoc uong

Ban can ho tro gi nao?";
    }

    private static string GetStatusText(string status)
    {
        return status switch
        {
            "NOW_SHOWING" => "Dang chieu",
            "UPCOMING" => "Sap ra mat",
            "ARCHIVED" => "Da ngung chieu",
            _ => status
        };
    }

    private static string GetCategoryName(string category)
    {
        return category switch
        {
            "DRINK" => "🥤 Nuoc uong",
            "FOOD" => "🍕 Do an",
            "COMBO" => "✨ Combo",
            _ => category
        };
    }

    private static string GetCategoryEmoji(string category)
    {
        return category switch
        {
            "DRINK" => "🥤",
            "FOOD" => "🍿",
            "COMBO" => "🎁",
            _ => "•"
        };
    }
}
