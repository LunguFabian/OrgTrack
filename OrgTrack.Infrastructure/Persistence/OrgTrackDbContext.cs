using Microsoft.EntityFrameworkCore;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class OrgTrackDbContext : DbContext
{
    public OrgTrackDbContext(DbContextOptions<OrgTrackDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<UserUnitRole> UserUnitRoles => Set<UserUnitRole>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRsvp> EventRsvps => Set<EventRsvp>();
    public DbSet<EventInvitedUnit> EventInvitedUnits => Set<EventInvitedUnit>();
    public DbSet<EventInvitedUser> EventInvitedUsers => Set<EventInvitedUser>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<InviteLink> InviteLinks => Set<InviteLink>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventInvitedUnit>()
            .HasKey(e => new { e.EventId, e.OrganizationUnitId });

        modelBuilder.Entity<EventInvitedUser>()
            .HasKey(e => new { e.EventId, e.UserId });
        modelBuilder.Entity<OrganizationUnit>()
            .HasOne(ou => ou.ParentUnit)
            .WithMany(ou => ou.ChildUnits)
            .HasForeignKey(ou => ou.ParentUnitId)
            .OnDelete(DeleteBehavior.Restrict); // Previne ștergerea accidentală a întregului arbore
        modelBuilder.Entity<UserUnitRole>()
            .HasIndex(uur => new { uur.UserId, uur.OrganizationUnitId, uur.RoleId })
            .IsUnique(); // Un user nu poate avea același rol de două ori în același departament

        modelBuilder.Entity<UserUnitRole>()
            .HasOne(uur => uur.User)
            .WithMany(u => u.UnitRoles)
            .HasForeignKey(uur => uur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserUnitRole>()
            .HasOne(uur => uur.OrganizationUnit)
            .WithMany(ou => ou.Members)
            .HasForeignKey(uur => uur.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserUnitRole>()
            .HasOne(uur => uur.Role)
            .WithMany(r => r.UserUnitRoles)
            .HasForeignKey(uur => uur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull); // Dacă userul pleacă, task-ul rămâne neasignat

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Creator)
            .WithMany()
            .HasForeignKey(t => t.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.ParentTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.OrganizationUnit)
            .WithMany(ou => ou.Tasks)
            .HasForeignKey(t => t.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Event>()
            .HasOne(e => e.OrganizationUnit)
            .WithMany(ou => ou.Events)
            .HasForeignKey(e => e.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventRsvp>()
            .HasIndex(e => new { e.EventId, e.UserId })
            .IsUnique();

        modelBuilder.Entity<EventRsvp>()
            .HasOne(e => e.Event)
            .WithMany(ev => ev.Rsvps)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventRsvp>()
            .HasOne(e => e.User)
            .WithMany(u => u.EventRsvps)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ActivityLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(a => a.OrganizationUnit)
            .WithMany()
            .HasForeignKey(a => a.OrganizationUnitId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<InviteLink>()
            .HasIndex(i => i.Token)
            .IsUnique();

        modelBuilder.Entity<InviteLink>()
            .HasOne(i => i.OrganizationUnit)
            .WithMany()
            .HasForeignKey(i => i.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InviteLink>()
            .HasOne(i => i.Role)
            .WithMany()
            .HasForeignKey(i => i.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InviteLink>()
            .HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrganizationUnit>().Property(e => e.Type).HasConversion<string>();
        modelBuilder.Entity<OrganizationUnit>().Property(e => e.DepartmentType).HasConversion<string>();
        modelBuilder.Entity<TaskItem>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<TaskItem>().Property(e => e.Priority).HasConversion<string>();
        modelBuilder.Entity<EventRsvp>().Property(e => e.Status).HasConversion<string>();
    }
}