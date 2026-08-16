# Volunteer/Rota Scheduling System

A comprehensive ASP.NET Core MVC application for managing volunteer assignments and scheduling for ministry teams.

## Features Implemented

### Core Entity System (Day 1-2)
- **Volunteer**: Name, email, phone, active status, availability notes
- **Role**: Name, description, active status
- **Service**: Name, date, start/end time, active status
- **Assignment**: Links volunteers to roles in services with notes and notification tracking
- **Unavailability**: Blackout date ranges for volunteers with optional reason
- **VolunteerRole**: Many-to-many relationship between volunteers and roles

### Database & Seeding
- EF Core with SQL Server
- Fluent API configuration for relationships and cascade delete behavior
- Sample data seeding with realistic test data (volunteers, roles, 4 weeks of services)

### CRUD Operations (Day 3-5)
- **Volunteers**: Full CRUD with duplicate contact validation
- **Roles**: Full CRUD operations
- **Services**: Full CRUD plus bulk "Generate Services" feature for recurring Sunday services
- **Unavailability**: Manage volunteer blackout periods

### Advanced Scheduling Logic (Day 6-9)
- **Role Eligibility Filtering**: Only show volunteers qualified for specific roles
- **Availability Checking**: Filter out volunteers with conflicting unavailability periods
- **Double-Booking Prevention**: Ensure volunteers aren't assigned to multiple roles in the same service
- **Fairness Rotation Algorithm**: 
  - Ranks volunteers by least-recently-served per role
  - 90-day lookback window for balanced assignments
  - Weights fairness per-role (not overall count)
  - Manual override capability for administrators

### User Interface (Day 10)
- **Weekly/Monthly Calendar View**: Visual overview of all assignments
- **Unfilled Role Indicators**: Red highlighting for roles needing volunteers
- **Responsive Design**: CSS grid-based layout without heavy JS libraries

### Notification System (Day 11)
- Interface-based design (`INotificationService`)
- Email notification implementation (log-based, ready for real email integration)
- Manual notification resend capability

### Admin Dashboard (Day 12)
- Summary of unfilled upcoming slots
- Volunteers overdue for rotation (not served in 60+ days)
- Actionable view tying together assignment and rotation logic

### Testing & Validation (Day 13)
- End-to-end functionality testing
- Edge case handling (off-by-one errors in rotation lookback)
- Business rule enforcement at service layer
- Proper validation throughout the application

## Architecture

```
RotaScheduler/
├── Controllers/          # MVC Controllers
├── Models/
│   └── Entities/        # EF Core entities
├── ViewModels/          # View-specific models
├── Views/              # Razor views
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbSeeder.cs     # Sample data seeding
├── Services/           # Business logic layer
│   ├── IAssignmentService.cs
│   ├── AssignmentService.cs
│   ├── INotificationService.cs
│   ├── NotificationService.cs
│   ├── IDashboardService.cs
│   └── DashboardService.cs
└── Program.cs          # Application entry point
```

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Configuration

1. Update `appsettings.json` with your connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RotaScheduler;Trusted_Connection=True;"
  }
}
```

2. Apply database migrations:
```bash
dotnet ef database update
```

3. Seed sample data (automatic on first run):
```bash
dotnet run
```

### Running the Application

```bash
cd RotaScheduler
dotnet run
```

Navigate to `https://localhost:7001` (or the URL shown in console).

## Key Design Decisions

1. **Service Layer Pattern**: Business logic (eligibility filtering, rotation algorithm) is in dedicated service classes, not controllers, for testability and reusability.

2. **Interface-Based Notifications**: `INotificationService` allows swapping delivery mechanisms without touching core assignment logic.

3. **Per-Role Fairness**: Rotation algorithm tracks last-served dates per role, not overall assignment count, so someone over-scheduled as an usher still ranks high for media roles.

4. **90-Day Lookback Window**: Prevents the algorithm from being skewed by very old assignments while still maintaining fairness.

5. **Manual Override**: Admins can assign any eligible volunteer regardless of the suggestion, providing flexibility for special circumstances.

6. **Lightweight Calendar**: Uses Razor + CSS grid instead of heavy JavaScript calendar libraries for better performance and simpler maintenance.

## API Endpoints

| Controller | Actions |
|------------|---------|
| Volunteers | Index, Create, Edit, Delete, Details |
| Roles | Index, Create, Edit, Delete, Details |
| Services | Index, Create, Edit, Delete, Details, GenerateServices |
| Assignments | Index, Create, Edit, Delete, Details, SendNotification |
| Unavailabilities | Index, Create, Edit, Delete, Details |
| Calendar | Index (weekly/monthly views) |
| Dashboard | Index |

## Future Enhancements

- Real email integration (SendGrid, SMTP)
- Volunteer self-service portal
- Recurrence engine for complex service patterns
- Export to calendar formats (iCal, Google Calendar)
- Mobile-responsive improvements
- Unit test suite
