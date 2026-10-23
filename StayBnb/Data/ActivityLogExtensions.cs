using StayBnb.Models;

namespace StayBnb.Data
{
    public static class ActivityLogExtensions
    {
        // Adds an activity log entry for a user, which is saved on the next SaveChangesAsync call.
        public static void LogActivity(this ApplicationDbContext db, string? userId, string action, ActivityType type)
        {
            db.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = action, ActivityType = type });
        }
    }
}
