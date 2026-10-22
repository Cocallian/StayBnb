using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Inbox()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            // Opening the inbox marks everything as read; the view still highlights what was new
            var unread = messages.Where(m => !m.IsRead).ToList();
            ViewBag.UnreadIds = unread.Select(m => m.Id).ToHashSet();
            if (unread.Count > 0)
            {
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return View(messages);
        }

        public async Task<IActionResult> Compose()
        {
            await LoadRecipientsAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(string receiverId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            content = (content ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(receiverId) || receiverId == userId || !await _context.Users.AnyAsync(u => u.Id == receiverId))
                ModelState.AddModelError("receiverId", "Choose someone to send the message to.");
            if (content.Length == 0)
                ModelState.AddModelError("content", "Write a message before sending.");
            else if (content.Length > 4000)
                ModelState.AddModelError("content", "The message must be 4000 characters or fewer.");

            if (!ModelState.IsValid)
            {
                await LoadRecipientsAsync(receiverId);
                ViewBag.Content = content;
                return View();
            }

            _context.Messages.Add(new Message { SenderId = userId, ReceiverId = receiverId, Content = content });
            _context.Notifications.Add(new Notification { UserId = receiverId, Title = "New Message", Type = NotificationType.Message });
            _context.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = "MessageSent", ActivityType = ActivityType.Message });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(History));
        }

        // Everyone except the logged-in user can receive a message
        private async Task LoadRecipientsAsync(string? selected = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Recipients = await _context.Users
                .Where(u => u.Id != userId)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .ToListAsync();
            ViewBag.Selected = selected;
        }

        // Every message the logged-in user has sent or received, oldest first (the view groups them by the other person)
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
            return View(messages);
        }
    }
}
