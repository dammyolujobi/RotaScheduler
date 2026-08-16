using Microsoft.EntityFrameworkCore;
using RotaScheduler.Models.Entities;

namespace RotaScheduler.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (context.Roles.Any()) return; // Already seeded

        // Seed Roles
        var roles = new List<Role>
        {
            new() { Name = "Usher", Description = "Greet and seat congregation members" },
            new() { Name = "Worship Leader", Description = "Lead worship songs" },
            new() { Name = "Sound Technician", Description = "Manage audio equipment" },
            new() { Name = "Children's Ministry", Description = "Supervise children during service" },
            new() { Name = "Media", Description = "Handle projection and livestream" },
            new() { Name = "Communion Server", Description = "Serve communion elements" }
        };
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        // Seed Volunteers
        var volunteers = new List<Volunteer>
        {
            new() { Name = "John Smith", Email = "john.smith@email.com", Phone = "555-0101", AvailabilityNotes = "Available most Sundays" },
            new() { Name = "Mary Johnson", Email = "mary.johnson@email.com", Phone = "555-0102", AvailabilityNotes = "Prefers morning services" },
            new() { Name = "Robert Williams", Email = "robert.williams@email.com", Phone = "555-0103", AvailabilityNotes = "Available twice a month" },
            new() { Name = "Patricia Brown", Email = "patricia.brown@email.com", Phone = "555-0104", AvailabilityNotes = "Available for special events" },
            new() { Name = "Michael Davis", Email = "michael.davis@email.com", Phone = "555-0105", AvailabilityNotes = "Full availability" },
            new() { Name = "Linda Miller", Email = "linda.miller@email.com", Phone = "555-0106", AvailabilityNotes = "Unavailable first Sunday of month" },
            new() { Name = "David Wilson", Email = "david.wilson@email.com", Phone = "555-0107", AvailabilityNotes = "Available evenings only" },
            new() { Name = "Elizabeth Moore", Email = "elizabeth.moore@email.com", Phone = "555-0108", AvailabilityNotes = "Available most weeks" },
            new() { Name = "James Taylor", Email = "james.taylor@email.com", Phone = "555-0109", AvailabilityNotes = "New volunteer" },
            new() { Name = "Jennifer Anderson", Email = "jennifer.anderson@email.com", Phone = "555-0110", AvailabilityNotes = "Experienced server" }
        };
        context.Volunteers.AddRange(volunteers);
        await context.SaveChangesAsync();

        // Assign volunteers to roles
        var volunteerRoles = new List<VolunteerRole>
        {
            new() { VolunteerId = 1, RoleId = 1 }, // John - Usher
            new() { VolunteerId = 1, RoleId = 6 }, // John - Communion Server
            new() { VolunteerId = 2, RoleId = 2 }, // Mary - Worship Leader
            new() { VolunteerId = 3, RoleId = 3 }, // Robert - Sound Tech
            new() { VolunteerId = 3, RoleId = 5 }, // Robert - Media
            new() { VolunteerId = 4, RoleId = 4 }, // Patricia - Children's Ministry
            new() { VolunteerId = 5, RoleId = 1 }, // Michael - Usher
            new() { VolunteerId = 5, RoleId = 3 }, // Michael - Sound Tech
            new() { VolunteerId = 6, RoleId = 2 }, // Linda - Worship Leader
            new() { VolunteerId = 7, RoleId = 5 }, // David - Media
            new() { VolunteerId = 8, RoleId = 4 }, // Elizabeth - Children's Ministry
            new() { VolunteerId = 8, RoleId = 6 }, // Elizabeth - Communion Server
            new() { VolunteerId = 9, RoleId = 1 }, // James - Usher
            new() { VolunteerId = 10, RoleId = 1 }, // Jennifer - Usher
            new() { VolunteerId = 10, RoleId = 4 }  // Jennifer - Children's Ministry
        };
        context.VolunteerRoles.AddRange(volunteerRoles);
        await context.SaveChangesAsync();

        // Seed Services (4 weeks of Sunday services)
        var startDate = GetNextSunday();
        var services = new List<Service>();
        
        for (int i = 0; i < 4; i++)
        {
            var serviceDate = startDate.AddDays(i * 7);
            
            // Main Sunday Service
            services.Add(new Service
            {
                Name = "Sunday Morning Service",
                ServiceDate = serviceDate,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 30, 0)
            });

            // Evening Service
            services.Add(new Service
            {
                Name = "Sunday Evening Service",
                ServiceDate = serviceDate,
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(19, 30, 0)
            });
        }
        context.Services.AddRange(services);
        await context.SaveChangesAsync();

        // Seed some Unavailabilities
        var unavailabilities = new List<Unavailability>
        {
            new() { VolunteerId = 6, StartDate = startDate.AddDays(-7), EndDate = startDate.AddDays(14), Reason = "Vacation" },
            new() { VolunteerId = 3, StartDate = startDate.AddDays(14), EndDate = startDate.AddDays(21), Reason = "Business trip" }
        };
        context.Unavailabilities.AddRange(unavailabilities);
        await context.SaveChangesAsync();
    }

    private static DateTime GetNextSunday()
    {
        var today = DateTime.Today;
        int daysUntilSunday = (7 - (int)today.DayOfWeek) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7; // Get next Sunday, not today
        return today.AddDays(daysUntilSunday);
    }
}
