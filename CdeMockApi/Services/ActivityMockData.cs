using System.Text.Json;
using CdeMockApi.Models;

namespace CdeMockApi.Services;

// One instance per mock API: catalog is loaded once; bookings remain in memory.
public sealed class ActivityMockData
{
    private readonly Dictionary<string, PackageData> _packages;

    public IReadOnlyList<Activity> Activities { get; }
    public string CurrencyCode { get; }
    public object Sync { get; } = new();
    public Dictionary<string, ActivityBooking> Bookings { get; } = new();
    public Dictionary<string, string> IdempotencyKeys { get; } = new();

    public ActivityMockData(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "activities.json");
        var seed = JsonSerializer.Deserialize<SeedData>(
            File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Mock catalog '{path}' is empty.");

        if (seed.Activities == null || seed.Packages == null ||
            string.IsNullOrWhiteSpace(seed.CurrencyCode) ||
            seed.Packages.Values.Any(p => p == null || p.Inventory < 0 || p.AdultRate < 0 || p.ChildRate < 0))
            throw new InvalidOperationException($"Mock catalog '{path}' has invalid package data or currency.");

        Activities = seed.Activities.AsReadOnly();
        CurrencyCode = seed.CurrencyCode;
        _packages = new Dictionary<string, PackageData>(seed.Packages, StringComparer.OrdinalIgnoreCase);
        foreach (var activity in Activities)
        {
            if (activity == null || activity.TimeSlots == null || activity.TimeSlots.Count == 0 ||
                activity.Packages == null ||
                activity.Packages.Any(package => package == null || !_packages.ContainsKey(package.PackageId)))
                throw new InvalidOperationException($"Mock catalog '{path}' has missing time slots or package pricing/inventory data.");
        }
    }

    public decimal GetAdultRate(string packageId) => _packages[packageId].AdultRate;
    public decimal GetChildRate(string packageId) => _packages[packageId].ChildRate;

    public int GetRemainingSeats(string activityId, string packageId, DateOnly date, string timeSlot)
    {
        lock (Sync)
        {
            int reserved = Bookings.Values.Where(b =>
                b.Status == "Confirmed" && b.ActivityId == activityId && b.PackageId == packageId &&
                b.Date == date && b.TimeSlot == timeSlot).Sum(b => b.Adults + b.Children);
            return Math.Max(0, _packages[packageId].Inventory - reserved);
        }
    }

    private sealed class SeedData
    {
        public required string CurrencyCode { get; init; }
        public required List<Activity> Activities { get; init; }
        public required Dictionary<string, PackageData> Packages { get; init; }
    }

    private sealed class PackageData
    {
        public required int Inventory { get; init; }
        public required decimal AdultRate { get; init; }
        public required decimal ChildRate { get; init; }
    }
}
