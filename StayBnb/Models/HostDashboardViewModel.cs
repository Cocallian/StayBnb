namespace StayBnb.Models
{
    public class HostDashboardViewModel
    {
        public int TotalProperties { get; set; }
        public int PendingRequests { get; set; }
        public int UpcomingBookings { get; set; }
        public decimal Earnings { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<Booking> RecentBookings { get; set; } = new();
    }
}
