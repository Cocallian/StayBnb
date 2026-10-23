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
        // Shows messages sent to the user, newest first, and marks them all as read.
        public async Task<IActionResult> Inbox()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            var unread = messages.Where(m => !m.IsRead).ToList();
            ViewBag.UnreadIds = unread.Select(m => m.Id).ToHashSet();
            if (unread.Count > 0)
            {
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return View(messages);
        }

        // Shows the new message form with a list of people the user can write to.
        public async Task<IActionResult> Compose()
        {
            await LoadRecipientsAsync();
            return View();
        }

        // Checks the receiver and text, then saves the message with a notification and log entry.
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

        // Loads every user except the current one into the ViewBag so they can be picked as the receiver.
        private async Task LoadRecipientsAsync(string? selected = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Recipients = await _context.Users
                .Where(u => u.Id != userId)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .ToListAsync();
            ViewBag.Selected = selected;
        }

        // Shows every message the user has sent or received, oldest first, so conversations can be read.
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
