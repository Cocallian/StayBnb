using StayBnb.Models;

namespace StayBnb.Data
{
    public static class ActivityLogExtensions
    {
        // Queues an activity log entry; it is saved with the caller's next SaveChangesAsync
        public static void LogActivity(this ApplicationDbContext db, string? userId, string action, ActivityType type)
        {
            db.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = action, ActivityType = type });
        }
    }
}
