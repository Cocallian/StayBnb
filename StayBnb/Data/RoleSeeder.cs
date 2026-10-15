using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StayBnb.Models;

namespace StayBnb.Data
{
    /// <summary>
    /// Runs on application startup. Applies migrations, then seeds:
    /// roles, the SuperAdmin, an Admin, 3 Hosts, 10 Guests, amenities, 12 properties (with images and
    /// check-in processes), host applications, bookings, payments, check-ins, reviews, wishlists,
    /// messages, notifications and activity logs.
    ///
    /// Safe to run repeatedly - each section only inserts when its data is missing.
    ///
    /// Seeded logins:
    ///   superadmin@staybnb.com / SuperAdmin@123
    ///   admin@staybnb.com      / Password@123
    ///   host1@staybnb.com .. host3@staybnb.com   / Password@123
    ///   guest1@staybnb.com .. guest10@staybnb.com / Password@123
    /// </summary>
    public static class RoleSeeder
    {
        private const string DefaultPassword = "Password@123";
        private const string SuperAdminPassword = "SuperAdmin@123";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await db.Database.MigrateAsync();

            await SeedRolesAsync(roleManager);
            await SeedAmenitiesAsync(db);

            // Users
            await EnsureUserAsync(userManager, "superadmin@staybnb.com", "Super", "Admin", Roles.SuperAdmin, SuperAdminPassword);
            await EnsureUserAsync(userManager, "admin@staybnb.com", "Lunga", "Aphane", Roles.Admin);

            var hostNames = new[] { ("Elsie", "Masuku"), ("Muzi", "Mango"), ("Trevor", "Mlotshwa") };
            var hosts = new List<ApplicationUser>();
            for (var i = 0; i < hostNames.Length; i++)
                hosts.Add(await EnsureUserAsync(userManager, $"host{i + 1}@staybnb.com", hostNames[i].Item1, hostNames[i].Item2, Roles.Host));

            var guestNames = new[]
            {
                ("Banele", "Mthembu"), ("Sboniso", "Mango"), ("Nthabiseng", "Mangi"), ("Neroshen", "Govender"),
                ("Thandi", "Nkosi"), ("Pieter", "van Wyk"), ("Aisha", "Patel"), ("Sipho", "Dlamini"),
                ("Lerato", "Molefe"), ("Johan", "Botha")
            };
            var guests = new List<ApplicationUser>();
            for (var i = 0; i < guestNames.Length; i++)
                guests.Add(await EnsureUserAsync(userManager, $"guest{i + 1}@staybnb.com", guestNames[i].Item1, guestNames[i].Item2, Roles.Guest));

            if (await db.HostProperties.AnyAsync()) return;

            var amenities = await db.Amenities.OrderBy(a => a.Id).ToListAsync();
            var properties = await SeedPropertiesAsync(db, hosts, guests, amenities);
            var bookings = await SeedBookingsAsync(db, properties, guests);
            await SeedReviewsAsync(db, bookings);
            await SeedWishlistsAsync(db, properties, guests);
            await SeedCommunicationAsync(db, hosts, guests, bookings);
            await SeedActivityLogsAsync(db, hosts, guests);
        }

        // ---------------------------------------------------------------- roles & users

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { Roles.SuperAdmin, Roles.Admin, Roles.Host, Roles.Guest })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager, string email, string first, string last, string role, string password = DefaultPassword)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null) return user;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Seeding {email} failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        // ---------------------------------------------------------------- amenities

        private static async Task SeedAmenitiesAsync(ApplicationDbContext db)
        {
            if (await db.Amenities.AnyAsync()) return;

            db.Amenities.AddRange(
                new Amenity { Name = "Wifi", IconClass = "bi-wifi" },
                new Amenity { Name = "Kitchen", IconClass = "bi-cup-hot" },
                new Amenity { Name = "Washer", IconClass = "bi-water" },
                new Amenity { Name = "Dryer", IconClass = "bi-wind" },
                new Amenity { Name = "Air Conditioning", IconClass = "bi-snow" },
                new Amenity { Name = "Heating", IconClass = "bi-thermometer-sun" },
                new Amenity { Name = "Free Parking", IconClass = "bi-car-front" },
                new Amenity { Name = "Pool", IconClass = "bi-droplet" },
                new Amenity { Name = "TV", IconClass = "bi-tv" },
                new Amenity { Name = "Braai / BBQ", IconClass = "bi-fire" },
                new Amenity { Name = "Backup Power", IconClass = "bi-lightning-charge" },
                new Amenity { Name = "Garden", IconClass = "bi-tree" });
            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- properties

        private record PropertySeed(string Title, string City, string Address, decimal Price, decimal Cleaning, decimal Service, string Description);

        private static async Task<List<HostProperty>> SeedPropertiesAsync(
            ApplicationDbContext db, List<ApplicationUser> hosts, List<ApplicationUser> guests, List<Amenity> amenities)
        {
            var seeds = new[]
            {
                new PropertySeed("Luxury Apartment", "Durban", "14 Marine Parade, Durban", 1320, 250, 150, "Modern 2-bedroom apartment on the beachfront with a sea-facing balcony, secure parking and fast fibre wifi."),
                new PropertySeed("Exceptional Villa in Drakensberg", "Drakensberg", "3 Champagne Valley Rd, Drakensberg", 1320, 400, 200, "Spacious mountain villa with a plunge pool, wood-fired braai and sweeping views of the escarpment."),
                new PropertySeed("Cozy 3 Bedroom House", "Nelspruit", "22 Jacaranda Street, Nelspruit", 825, 200, 120, "Family-friendly home close to Kruger National Park with a large garden and covered patio."),
                new PropertySeed("Cozy Holiday Cabin", "Mpumalanga", "8 Panorama Route, Graskop", 1230, 250, 130, "Warm timber cabin with a fireplace and deck, minutes from God's Window and Bourke's Luck Potholes."),
                new PropertySeed("Exceptional Holiday Home", "Nelspruit", "5 Crocodile River Dr, Nelspruit", 1120, 300, 150, "Open-plan holiday home with a pool, braai area and backup power for uninterrupted stays."),
                new PropertySeed("Beautiful Beachfront Home", "Umhlanga", "101 Lagoon Drive, Umhlanga", 2100, 450, 250, "Stylish beachfront home with direct beach access, four bedrooms and ocean views from every room."),
                new PropertySeed("Lovely City Apartment", "Umhlanga", "9 Chartwell Drive, Umhlanga", 1780, 250, 150, "Light-filled city apartment with floor-to-ceiling windows, a gym and a rooftop pool in the building."),
                new PropertySeed("Cozy Forest Cabin", "Durban", "27 Kloof Falls Road, Kloof", 3100, 350, 200, "Escape to our cozy forest cabin among towering trees and birdsong. Rustic charm, soft lighting and a relaxing deck."),
                new PropertySeed("Table Mountain View Loft", "Cape Town", "40 Kloof Street, Gardens, Cape Town", 2450, 300, 180, "Designer loft in the heart of Gardens with views of Table Mountain and walking distance to restaurants."),
                new PropertySeed("Camps Bay Sunset Studio", "Cape Town", "18 Victoria Road, Camps Bay", 1950, 220, 150, "Compact studio steps from Camps Bay beach with a private terrace made for sunsets."),
                new PropertySeed("Winelands Cottage Retreat", "Stellenbosch", "7 Helshoogte Pass, Stellenbosch", 1650, 280, 160, "Charming vineyard cottage with a fireplace, garden and wine-tasting rooms a short walk away."),
                new PropertySeed("Sandton Executive Suite", "Johannesburg", "60 Rivonia Road, Sandton", 1450, 200, 140, "Secure executive suite near Sandton City, ideal for business travellers, with a desk, wifi and backup power.")
            };

            var properties = new List<HostProperty>();
            for (var i = 0; i < seeds.Length; i++)
            {
                var s = seeds[i];
                var host = hosts[i % hosts.Count];
                var key = s.Title.ToLowerInvariant().Replace(' ', '-');

                var property = new HostProperty
                {
                    Title = s.Title,
                    City = s.City,
                    Address = s.Address,
                    Description = s.Description,
                    PricePerNight = s.Price,
                    CleaningFee = s.Cleaning,
                    ServiceFee = s.Service,
                    HostId = host.Id,
                    IsActive = true,
                    // Each property gets 6 amenities, rotating through the list.
                    Amenities = Enumerable.Range(0, 6).Select(n => amenities[(i + n * 2) % amenities.Count]).Distinct().ToList(),
                    Images = Enumerable.Range(1, 4)
                        .Select(n => new PropertyImage { ImageUrl = $"https://picsum.photos/seed/{key}-{n}/900/600" })
                        .ToList(),
                    CheckInProcess = new CheckInProcess
                    {
                        Title = $"Check-in for {s.Title}",
                        StepsJson = JsonSerializer.Serialize(new[]
                        {
                            new { Title = "Confirm arrival", Instructions = "Confirm your arrival time and number of guests.", RequiresDocument = false },
                            new { Title = "Upload ID or Passport", Instructions = "Upload a clear copy of your South African ID or passport for verification.", RequiresDocument = true },
                            new { Title = "Accept house rules", Instructions = "No smoking indoors, no parties, quiet hours from 22:00.", RequiresDocument = false }
                        })
                    }
                };
                db.HostProperties.Add(property);
                properties.Add(property);
            }

            // One extra property still awaiting Admin review (submitted by a Guest who applied to become a Host).
            var pendingApplicant = guests[7];
            var pendingProperty = new HostProperty
            {
                Title = "Garden Route Lodge",
                City = "Knysna",
                Address = "15 Lagoon View Road, Knysna",
                Description = "Peaceful lodge overlooking the Knysna lagoon with a deck, braai and two en-suite bedrooms.",
                PricePerNight = 1580,
                CleaningFee = 250,
                ServiceFee = 140,
                HostId = pendingApplicant.Id,
                IsActive = false,
                Images = { new PropertyImage { ImageUrl = "https://picsum.photos/seed/garden-route-lodge-1/900/600" } }
            };
            db.HostProperties.Add(pendingProperty);

            await db.SaveChangesAsync();

            // Host applications: every live property was approved; the lodge is pending review.
            foreach (var p in properties)
            {
                db.HostApplications.Add(new HostApplication
                {
                    ApplicationUserId = p.HostId,
                    PropertyId = p.Id,
                    Status = ApplicationStatus.Approved,
                    AppliedAt = DateTime.UtcNow.AddDays(-80)
                });
            }
            db.HostApplications.Add(new HostApplication
            {
                ApplicationUserId = pendingApplicant.Id,
                PropertyId = pendingProperty.Id,
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow.AddDays(-1)
            });

            // Notifications that go with the applications.
            foreach (var host in hosts)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = host.Id,
                    Title = "Host Application Approved",
                    Type = NotificationType.HostApplication,
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-79)
                });
            }
            db.Notifications.Add(new Notification
            {
                UserId = pendingApplicant.Id,
                Title = "Host Application Submitted",
                Type = NotificationType.HostApplication,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

            await db.SaveChangesAsync();
            return properties;
        }

        // ---------------------------------------------------------------- bookings, payments, check-ins

        private static async Task<List<Booking>> SeedBookingsAsync(ApplicationDbContext db, List<HostProperty> properties, List<ApplicationUser> guests)
        {
            var today = DateTime.Today;

            // (guest index, property index, start offset in days, nights, status, check-in status or null)
            var plan = new (int Guest, int Prop, int Start, int Nights, BookingStatus Status, CheckInStatus? CheckIn)[]
            {
                (0, 0, -30, 3, BookingStatus.Completed, CheckInStatus.Verified),
                (1, 1, -20, 3, BookingStatus.Completed, CheckInStatus.Verified),
                (2, 2, -45, 3, BookingStatus.Completed, CheckInStatus.Verified),
                (3, 3, -15, 2, BookingStatus.Completed, CheckInStatus.Verified),
                (5, 0, -60, 3, BookingStatus.Completed, CheckInStatus.Verified),
                (4, 4, -1, 3, BookingStatus.CheckedIn, CheckInStatus.Verified),
                (6, 6, 3, 2, BookingStatus.Approved, CheckInStatus.Submitted),
                (5, 5, 5, 3, BookingStatus.Approved, null),
                (1, 11, 30, 4, BookingStatus.Approved, null),
                (7, 7, 10, 2, BookingStatus.Pending, null),
                (8, 8, 20, 3, BookingStatus.Pending, null),
                (9, 9, 7, 2, BookingStatus.Rejected, null),
                (0, 10, 14, 2, BookingStatus.Cancelled, null)
            };

            var bookings = new List<Booking>();
            foreach (var (g, p, start, nights, status, checkInStatus) in plan)
            {
                var property = properties[p];
                var total = (property.PricePerNight * nights) + property.CleaningFee + property.ServiceFee;

                var booking = new Booking
                {
                    PropertyId = property.Id,
                    GuestId = guests[g].Id,
                    CheckInDate = today.AddDays(start),
                    CheckOutDate = today.AddDays(start + nights),
                    NumberOfGuests = 2,
                    TotalPrice = total,
                    Status = status,
                    Payment = new Payment
                    {
                        Amount = total,
                        Status = status switch
                        {
                            BookingStatus.Pending => PaymentStatus.Pending,
                            BookingStatus.Rejected or BookingStatus.Cancelled => PaymentStatus.Refunded,
                            _ => PaymentStatus.Paid
                        },
                        CreatedAt = today.AddDays(start - 7)
                    }
                };

                if (checkInStatus.HasValue)
                {
                    var checkIn = new GuestCheckIn
                    {
                        CheckInProcessId = property.CheckInProcess!.Id,
                        Status = checkInStatus.Value,
                        CreatedAt = today.AddDays(start - 2)
                    };
                    checkIn.Documents.Add(new GuestDocument
                    {
                        DocumentType = g % 2 == 0 ? "ID" : "Passport",
                        FileName = $"{guests[g].FirstName.ToLowerInvariant()}-{(g % 2 == 0 ? "id" : "passport")}.pdf",
                        Status = checkInStatus == CheckInStatus.Verified ? DocumentStatus.Verified : DocumentStatus.Pending
                    });
                    booking.GuestCheckIn = checkIn;
                }

                db.Bookings.Add(booking);
                bookings.Add(booking);
            }
            await db.SaveChangesAsync();

            // Notifications and activity log entries for the booking lifecycle.
            foreach (var b in bookings)
            {
                var property = properties.First(x => x.Id == b.PropertyId);
                db.Notifications.Add(new Notification
                {
                    UserId = property.HostId,
                    Title = "New Booking Request",
                    Type = NotificationType.Booking,
                    IsRead = b.Status != BookingStatus.Pending,
                    CreatedAt = b.Payment!.CreatedAt
                });

                if (b.Status is BookingStatus.Approved or BookingStatus.CheckedIn or BookingStatus.Completed)
                {
                    db.Notifications.Add(new Notification
                    {
                        UserId = b.GuestId,
                        Title = "Booking Approved",
                        Type = NotificationType.Success,
                        IsRead = b.Status == BookingStatus.Completed,
                        CreatedAt = b.Payment.CreatedAt.AddDays(1)
                    });
                }
                else if (b.Status == BookingStatus.Rejected)
                {
                    db.Notifications.Add(new Notification
                    {
                        UserId = b.GuestId,
                        Title = "Booking Rejected",
                        Type = NotificationType.Warning,
                        CreatedAt = b.Payment.CreatedAt.AddDays(1)
                    });
                }

                db.ActivityLogs.Add(new ActivityLog
                {
                    UserId = b.GuestId,
                    Action = "BookingCreated",
                    ActivityType = ActivityType.Booking,
                    CreatedAt = b.Payment.CreatedAt
                });
                if (b.Status != BookingStatus.Pending)
                {
                    db.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = property.HostId,
                        Action = $"Booking{b.Status}",
                        ActivityType = ActivityType.Booking,
                        CreatedAt = b.Payment.CreatedAt.AddDays(1)
                    });
                }
                if (b.GuestCheckIn != null)
                {
                    db.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = b.GuestId,
                        Action = $"CheckIn{b.GuestCheckIn.Status}",
                        ActivityType = ActivityType.CheckIn,
                        CreatedAt = b.GuestCheckIn.CreatedAt
                    });
                }
            }
            await db.SaveChangesAsync();
            return bookings;
        }

        // ---------------------------------------------------------------- reviews, wishlists

        private static async Task SeedReviewsAsync(ApplicationDbContext db, List<Booking> bookings)
        {
            var comments = new[]
            {
                (5, "Absolutely loved our stay. Spotless, exactly as pictured and the host was brilliant."),
                (4, "Great location and comfortable beds. Check-in was smooth. Would stay again."),
                (5, "Perfect getaway - peaceful, beautiful views and very well equipped."),
                (3, "Nice place overall but the wifi was a bit slow and the street was noisy at night."),
                (4, "Lovely home, lots of space for the family. Host responded quickly to every message.")
            };

            var i = 0;
            foreach (var b in bookings.Where(x => x.Status == BookingStatus.Completed))
            {
                var (rating, comment) = comments[i++ % comments.Length];
                db.Reviews.Add(new Review
                {
                    PropertyId = b.PropertyId,
                    ReviewerId = b.GuestId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = b.CheckOutDate.AddDays(1)
                });
            }
            await db.SaveChangesAsync();
        }

        private static async Task SeedWishlistsAsync(ApplicationDbContext db, List<HostProperty> properties, List<ApplicationUser> guests)
        {
            for (var g = 0; g < guests.Count; g++)
            {
                // Each guest saves two properties.
                db.WishlistItems.Add(new WishlistItem { UserId = guests[g].Id, PropertyId = properties[g % properties.Count].Id });
                db.WishlistItems.Add(new WishlistItem { UserId = guests[g].Id, PropertyId = properties[(g + 5) % properties.Count].Id });
            }
            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- messages, logs

        private static async Task SeedCommunicationAsync(ApplicationDbContext db, List<ApplicationUser> hosts, List<ApplicationUser> guests, List<Booking> bookings)
        {
            var now = DateTime.UtcNow;

            void Conversation(ApplicationUser guest, ApplicationUser host, string question, string answer, bool answerRead)
            {
                db.Messages.Add(new Message { SenderId = guest.Id, ReceiverId = host.Id, Content = question, Timestamp = now.AddDays(-2), IsRead = true });
                db.Messages.Add(new Message { SenderId = host.Id, ReceiverId = guest.Id, Content = answer, Timestamp = now.AddDays(-2).AddHours(1), IsRead = answerRead });
            }

            Conversation(guests[6], hosts[0], "Hi! Is early check-in possible on the day of arrival?", "Hello Aisha, yes - you can check in from 12:00 as the apartment is ready by then.", false);
            Conversation(guests[4], hosts[1], "Is there a braai and is firewood provided?", "Hi Thandi, there is a braai on the patio and we provide the first bag of wood.", true);
            Conversation(guests[7], hosts[2], "Do you allow small dogs at the property?", "Hi Sipho, small dogs are welcome. We just ask that they stay off the beds.", false);

            // A message from the Admin to a guest, plus the matching notifications.
            var admin = await db.Users.FirstAsync(u => u.Email == "admin@staybnb.com");
            db.Messages.Add(new Message
            {
                SenderId = admin.Id,
                ReceiverId = guests[7].Id,
                Content = "Welcome to Staybnb! Your host application is being reviewed and we will update you shortly.",
                Timestamp = now.AddHours(-5)
            });

            db.Notifications.Add(new Notification { UserId = guests[6].Id, Title = "New Message", Type = NotificationType.Message, CreatedAt = now.AddDays(-2).AddHours(1) });
            db.Notifications.Add(new Notification { UserId = guests[7].Id, Title = "New Message", Type = NotificationType.Message, CreatedAt = now.AddHours(-5) });
            db.Notifications.Add(new Notification { UserId = hosts[0].Id, Title = "Guest check-in submitted", Type = NotificationType.CheckIn, CreatedAt = now.AddHours(-3) });

            await db.SaveChangesAsync();
        }

        private static async Task SeedActivityLogsAsync(ApplicationDbContext db, List<ApplicationUser> hosts, List<ApplicationUser> guests)
        {
            var admin = await db.Users.FirstAsync(u => u.Email == "admin@staybnb.com");
            var superAdmin = await db.Users.FirstAsync(u => u.Email == "superadmin@staybnb.com");
            var now = DateTime.UtcNow;

            foreach (var g in guests)
                db.ActivityLogs.Add(new ActivityLog { UserId = g.Id, Action = "UserRegistered", ActivityType = ActivityType.Registration, CreatedAt = g.CreatedAt });

            foreach (var h in hosts)
            {
                db.ActivityLogs.Add(new ActivityLog { UserId = h.Id, Action = "HostApplicationSubmitted", ActivityType = ActivityType.HostApplication, CreatedAt = now.AddDays(-80) });
                db.ActivityLogs.Add(new ActivityLog { UserId = admin.Id, Action = "HostApplicationApproved", ActivityType = ActivityType.RoleChange, CreatedAt = now.AddDays(-79) });
                db.ActivityLogs.Add(new ActivityLog { UserId = h.Id, Action = "UserLoggedIn", ActivityType = ActivityType.Login, CreatedAt = now.AddDays(-1) });
            }

            db.ActivityLogs.Add(new ActivityLog { UserId = superAdmin.Id, Action = "AdminRoleAssigned", ActivityType = ActivityType.RoleChange, CreatedAt = now.AddDays(-85) });
            db.ActivityLogs.Add(new ActivityLog { UserId = guests[7].Id, Action = "HostApplicationSubmitted", ActivityType = ActivityType.HostApplication, CreatedAt = now.AddDays(-1) });
            db.ActivityLogs.Add(new ActivityLog { UserId = admin.Id, Action = "UserLoggedIn", ActivityType = ActivityType.Login, CreatedAt = now.AddHours(-6) });

            await db.SaveChangesAsync();
        }
    }
}
