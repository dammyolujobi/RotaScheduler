using RotaScheduler.Models.Entities;

namespace RotaScheduler.Services;

public interface INotificationService
{
    Task<bool> SendAssignmentNotificationAsync(Assignment assignment);
    Task<bool> SendAssignmentNotificationsAsync(IEnumerable<Assignment> assignments);
}

public class EmailNotificationService : INotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    // In production, this would use an actual email service like SendGrid, SMTP, etc.
    // For now, we log the notification (can be replaced with real email sending)

    public EmailNotificationService(ILogger<EmailNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendAssignmentNotificationAsync(Assignment assignment)
    {
        try
        {
            var volunteer = assignment.Volunteer;
            var role = assignment.Role;
            var service = assignment.Service;

            var subject = $"Volunteer Assignment: {role.Name} on {service.ServiceDate:MMM dd, yyyy}";
            var body = $@"
Dear {volunteer.Name},

You have been assigned to serve as **{role.Name}** for the **{service.Name}** on **{service.ServiceDate:dddd, MMMM dd, yyyy}**.

Service Details:
- Time: {service.StartTime:hh\\:mm} - {service.EndTime:hh\\:mm}
- Location: Main Campus

{(string.IsNullOrEmpty(assignment.Notes) ? "" : $"Notes: {assignment.Notes}")}

Thank you for your service!

Best regards,
Ministry Team
";

            // In production, send actual email here
            // For demo/testing, log the notification
            _logger.LogInformation("Email notification prepared for {VolunteerEmail}: {Subject}", 
                volunteer.Email, subject);
            _logger.LogDebug("Email body: {Body}", body);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send assignment notification for assignment {AssignmentId}", assignment.Id);
            return false;
        }
    }

    public async Task<bool> SendAssignmentNotificationsAsync(IEnumerable<Assignment> assignments)
    {
        var allSuccess = true;
        foreach (var assignment in assignments)
        {
            var success = await SendAssignmentNotificationAsync(assignment);
            if (!success)
                allSuccess = false;
        }
        return allSuccess;
    }
}
