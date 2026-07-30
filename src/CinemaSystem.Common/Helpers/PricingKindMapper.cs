using CinemaSystem.Common.Constants;
using CinemaSystem.Common.Enums;

namespace CinemaSystem.Common.Helpers;

public static class PricingKindMapper
{
    private static readonly Dictionary<string, RoomTypeKind> RoomTypeByLegacyCode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["STANDARD"] = RoomTypeKind.Standard,
            ["VIP"] = RoomTypeKind.Vip,
            ["IMAX"] = RoomTypeKind.Imax,
            ["4DX"] = RoomTypeKind.FourDx
        };

    private static readonly Dictionary<string, TimeSlotKind> TimeSlotByLegacyCode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["MORNING"] = TimeSlotKind.Normal,
            ["AFTERNOON"] = TimeSlotKind.Normal,
            ["EVENING"] = TimeSlotKind.Evening,
            ["MIDNIGHT"] = TimeSlotKind.Midnight,
            ["PEAK"] = TimeSlotKind.Peak
        };

    public static bool IsValidRoomTypeId(int roomTypeId)
        => Enum.IsDefined(typeof(RoomTypeKind), roomTypeId);

    public static bool IsValidTimeSlotId(int timeSlotId)
        => Enum.IsDefined(typeof(TimeSlotKind), timeSlotId);

    public static RoomTypeKind ToRoomTypeKind(int roomTypeId)
        => (RoomTypeKind)roomTypeId;

    public static TimeSlotKind ToTimeSlotKind(int timeSlotId)
        => (TimeSlotKind)timeSlotId;

    public static RoomTypeKind FromLegacyRoomType(string roomType)
    {
        if (RoomTypeByLegacyCode.TryGetValue(roomType.Trim(), out var kind))
        {
            return kind;
        }

        throw new InvalidOperationException(PricingRuleMessages.InvalidRoomTypeId);
    }

    public static TimeSlotKind FromLegacyTimeSlot(string timeSlot)
    {
        if (TimeSlotByLegacyCode.TryGetValue(timeSlot.Trim(), out var kind))
        {
            return kind;
        }

        throw new InvalidOperationException(PricingRuleMessages.InvalidTimeSlotId);
    }
}
