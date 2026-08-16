using Microsoft.EntityFrameworkCore;
using RotaScheduler.Models.Entities;

namespace RotaScheduler.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Unavailability> Unavailabilities => Set<Unavailability>();
    public DbSet<VolunteerRole> VolunteerRoles => Set<VolunteerRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Volunteer configuration
        modelBuilder.Entity<Volunteer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Service configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.ServiceDate, e.StartTime });
        });

        // Assignment configuration
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Volunteer)
                  .WithMany(v => v.Assignments)
                  .HasForeignKey(e => e.VolunteerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.Assignments)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Service)
                  .WithMany(s => s.Assignments)
                  .HasForeignKey(e => e.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ServiceId, e.RoleId });
            entity.HasIndex(e => e.VolunteerId);
        });

        // Unavailability configuration
        modelBuilder.Entity<Unavailability>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Volunteer)
                  .WithMany(v => v.Unavailabilities)
                  .HasForeignKey(e => e.VolunteerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // VolunteerRole configuration (many-to-many join table)
        modelBuilder.Entity<VolunteerRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Volunteer)
                  .WithMany(v => v.VolunteerRoles)
                  .HasForeignKey(e => e.VolunteerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.VolunteerRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.VolunteerId, e.RoleId }).IsUnique();
        });
    }
}
