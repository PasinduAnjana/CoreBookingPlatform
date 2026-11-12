using System.Text.Json;
using AbcMockApi.Models;

namespace AbcMockApi.Services;

// One instance per mock API: catalog is loaded once; bookings remain in memory.
public sealed class HotelMockData
{
    private readonly Dictionary<string, RoomData> _roomTypes;

    public IReadOnlyList<Hotel> Hotels { get; }
    public string Currency { get; }
    public object Sync { get; } = new();
    public Dictionary<string, HotelBooking> Bookings { get; } = new();
    public Dictionary<string, string> IdempotencyKeys { get; } = new();

    public HotelMockData(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "hotels.json");
        var seed = JsonSerializer.Deserialize<SeedData>(
            File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Mock catalog '{path}' is empty.");

        if (seed.Hotels == null || seed.RoomTypes == null ||
            string.IsNullOrWhiteSpace(seed.Currency) ||
            seed.RoomTypes.Values.Any(r => r == null || r.Inventory < 0 || r.NightlyRate < 0))
            throw new InvalidOperationException($"Mock catalog '{path}' has invalid room data or currency.");

        Hotels = seed.Hotels.AsReadOnly();
        Currency = seed.Currency;
        _roomTypes = new Dictionary<string, RoomData>(seed.RoomTypes, StringComparer.OrdinalIgnoreCase);
        foreach (var hotel in Hotels)
        {
            if (hotel == null || hotel.RoomTypes == null ||
                hotel.RoomTypes.Any(room => room == null || !_roomTypes.ContainsKey(room.RoomCode)))
                throw new InvalidOperationException($"Mock catalog '{path}' has a room without pricing/inventory data.");
        }
    }

    public decimal GetNightlyRate(string roomCode) => _roomTypes[roomCode].NightlyRate;

    public int GetRemainingRooms(string hotelCode, string roomCode, DateOnly checkIn, DateOnly checkOut)
    {
        lock (Sync)
        {
            var overlapping = Bookings.Values.Where(b =>
                b.Status == "Confirmed" && b.HotelCode == hotelCode && b.RoomCode == roomCode &&
                b.CheckIn < checkOut && b.CheckOut > checkIn).ToList();
            int maximumUsed = 0;
            for (var date = checkIn; date < checkOut; date = date.AddDays(1))
            {
                int used = overlapping.Where(b => b.CheckIn <= date && b.CheckOut > date).Sum(b => b.Rooms);
                maximumUsed = Math.Max(maximumUsed, used);
            }
            return Math.Max(0, _roomTypes[roomCode].Inventory - maximumUsed);
        }
    }

    private sealed class SeedData
    {
        public required string Currency { get; init; }
        public required List<Hotel> Hotels { get; init; }
        public required Dictionary<string, RoomData> RoomTypes { get; init; }
    }

    private sealed class RoomData
    {
        public required int Inventory { get; init; }
        public required decimal NightlyRate { get; init; }
    }
}
