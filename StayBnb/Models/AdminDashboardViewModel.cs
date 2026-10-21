namespace StayBnb.Models
{
    public class AdminDashboardViewModel
    {

        public List<ApplicationUser> Users { get; set; } = new();
        public List<ApplicationUser> Hosts { get; set; } = new();
        public List<HostApplication> HostApplications { get; set; } = new();
        public List<HostProperty> Properties { get; set; } = new();
        public List<Booking> Bookings { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public List<ActivityLog> RecentActivity { get; set; } = new();
        public int TotalUsers => Users.Count;
        // Same filter as the Host Requests table: pending, with an applicant, an image and a document
        public int PendingHostApplications => HostApplications.Count(a => a.Status == ApplicationStatus.Pending
            && a.ApplicationUser != null
            && !string.IsNullOrEmpty(a.ImageUrl));
        public int ActiveProperties => Properties.Count(p => p.IsActive);
        public decimal PlatformRevenue => Payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
        public int TotalBookings => Bookings.Count;
        public int PendingBookings => Bookings.Count(b => b.Status == BookingStatus.Pending);
        public int ApprovedHosts => Hosts.Count;
    }
}
