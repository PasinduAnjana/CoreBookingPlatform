namespace CdeMockApi.Models
{
    public class Activity
    {
        public string ActivityId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }

        public string Overview { get; set; } = string.Empty;

        public List<string> Gallery { get; set; } = [];
        public List<string> Includes { get; set; } = [];
        public List<string> Excludes { get; set; } = [];

        public List<ActivityPackage> Packages { get; set; } = [];
    }
    public class ActivityPackage
    {
        public string PackageId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public List<string> PassengerTypes { get; set; } = [];
    }
}

