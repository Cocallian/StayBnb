using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StayBnb.Models;

namespace StayBnb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<HostProperty> HostProperties => Set<HostProperty>();
        public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<HostApplication> HostApplications => Set<HostApplication>();
        public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
        public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
        public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<HostProperty>(e =>
            {
                e.Property(p => p.PricePerNight).HasColumnType("decimal(18,2)");
                e.Property(p => p.CleaningFee).HasColumnType("decimal(18,2)");
                e.Property(p => p.ServiceFee).HasColumnType("decimal(18,2)");
                e.HasOne(p => p.Host).WithMany(u => u.Properties).HasForeignKey(p => p.HostId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(p => p.Amenities).WithMany(a => a.Properties);
                e.HasOne(p => p.CheckInProcess).WithOne(c => c.Property).HasForeignKey<CheckInProcess>(c => c.PropertyId);
            });

            b.Entity<PropertyImage>()
                .HasOne(i => i.Property).WithMany(p => p.Images).HasForeignKey(i => i.PropertyId);

            b.Entity<Booking>(e =>
            {
                e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Property).WithMany(p => p.Bookings).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Guest).WithMany(u => u.Bookings).HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Payment).WithOne(p => p.Booking).HasForeignKey<Payment>(p => p.BookingId);
                e.HasOne(x => x.GuestCheckIn).WithOne(g => g.Booking).HasForeignKey<GuestCheckIn>(g => g.BookingId);
            });

            b.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(18,2)");

            b.Entity<Review>(e =>
            {
                e.HasOne(r => r.Property).WithMany(p => p.Reviews).HasForeignKey(r => r.PropertyId);
                e.HasOne(r => r.Reviewer).WithMany(u => u.Reviews).HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<WishlistItem>(e =>
            {
                e.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(w => w.Property).WithMany().HasForeignKey(w => w.PropertyId).OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<Message>(e =>
            {
                e.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<ActivityLog>().HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
            b.Entity<Notification>().HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

            b.Entity<HostApplication>(e =>
            {
                e.HasOne(a => a.ApplicationUser).WithMany().HasForeignKey(a => a.ApplicationUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Property).WithMany().HasForeignKey(a => a.PropertyId).OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<GuestCheckIn>().HasOne(g => g.CheckInProcess).WithMany().HasForeignKey(g => g.CheckInProcessId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<GuestDocument>().HasOne(d => d.GuestCheckIn).WithMany(g => g.Documents).HasForeignKey(d => d.GuestCheckInId);
        }
    }
}
