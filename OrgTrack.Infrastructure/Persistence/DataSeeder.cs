using Microsoft.EntityFrameworkCore;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Constants;
using OrgTrack.Domain.Enums;
using TaskStatus = OrgTrack.Domain.Enums.TaskStatus;

namespace OrgTrack.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedDataAsync(OrgTrackDbContext context, bool isDevelopment)
    {
        await SeedRolesAsync(context);

        if (isDevelopment)
        {
            await SeedDummyDataAsync(context);
        }
    }

    private static async Task SeedRolesAsync(OrgTrackDbContext context)
    {
        var rolesToSeed = new List<(string Name, string Description, string Permissions)>
        {
            ("NationalPresident",            "National President — full access across all committees",             "[\"All\"]"),
            ("NationalVicePresident",        "National VP — views their department across all committees",         "[\"Units.View\", \"Members.View\", \"Tasks.View\", \"Events.View\", \"Reports.View\"]"),
            ("NationalVicePresidentHR",      "National VP HR — manages members across all committees",             "[\"All.View\", \"Members.Manage\"]"),
            ("NationalVicePresidentFinance", "National VP Finance — manages finances across all committees",       "[\"All.View\", \"Finance.Manage\"]"),
            ("LocalPresident",               "Local Committee President — full access within their committee",     "[\"Units.Manage\", \"Members.Manage\", \"Tasks.Manage\", \"Events.Manage\", \"Reports.View\"]"),
            ("LocalVicePresident",           "Local VP — manages their department within the local committee",     "[\"Units.View\", \"Members.Manage\", \"Tasks.Manage\", \"Events.Manage\", \"Members.View\"]"),
            ("LocalVicePresidentHR",         "Local VP HR — manages members across all departments in committee",  "[\"All.View\", \"Members.Manage\"]"),
            ("LocalVicePresidentFinance",    "Local VP Finance — manages finances within committee",               "[\"All.View\", \"Finance.Manage\"]"),
            ("TeamLeader",                   "Team Leader — manages tasks and events for their team only",         "[\"Tasks.Manage\", \"Events.Manage\", \"Members.View\", \"Units.View\"]"),
            ("Member",                       "Team member — views own tasks and participates in events",           "[\"Tasks.ViewOwn\", \"Events.View\", \"Members.View\"]"),
        };

        foreach (var (name, description, permissions) in rolesToSeed)
        {
            var existing = await context.Roles.FirstOrDefaultAsync(r => r.Name == name);
            if (existing == null)
            {
                context.Roles.Add(new Role
                {
                    Name = name,
                    Description = description,
                    Permissions = permissions
                });
            }
            else
            {
                existing.Permissions = permissions;
                existing.Description = description;
                context.Roles.Update(existing);
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedDummyDataAsync(OrgTrackDbContext context)
    {
        if (await context.OrganizationUnits.AnyAsync(u => u.Name == "AIESEC in Iasi"))
        {
            return;
        }

        var adminEmail = "admin@aiesec.ro";
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        
        await context.ActivityLogs.ExecuteDeleteAsync();
        await context.EventRsvps.ExecuteDeleteAsync();
        await context.EventInvitedUnits.ExecuteDeleteAsync();
        await context.EventInvitedUsers.ExecuteDeleteAsync();
        await context.Events.ExecuteDeleteAsync();
        await context.Tasks.ExecuteDeleteAsync();
        await context.InviteLinks.ExecuteDeleteAsync();
        await context.UserUnitRoles.ExecuteDeleteAsync();
        await context.OrganizationUnits.ExecuteDeleteAsync();
        await context.Users.Where(u => u.Email != adminEmail).ExecuteDeleteAsync();

        if (admin == null)
        {
            admin = new User { FirstName = "Admin", LastName = "Super", Email = adminEmail, IsActive = true };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }

        var romania = new OrganizationUnit { Name = "AIESEC in Romania", Type = UnitType.National, Description = "National Member Committee" };
        var iasi = new OrganizationUnit { Name = "AIESEC in Iasi", Type = UnitType.Committee, ParentUnit = romania, Description = "Local Committee Iasi" };
        
        var mkt = new OrganizationUnit { Name = "Marketing", Type = UnitType.Department, DepartmentType = DepartmentType.Marketing, ParentUnit = iasi };
        var mktB2c = new OrganizationUnit { Name = "B2C MKT", Type = UnitType.Team, ParentUnit = mkt };
        var mktB2b = new OrganizationUnit { Name = "B2B MKT", Type = UnitType.Team, ParentUnit = mkt };
        
        var tm = new OrganizationUnit { Name = "Talent Management", Type = UnitType.Department, DepartmentType = DepartmentType.HR, ParentUnit = iasi };

        context.OrganizationUnits.AddRange(romania, iasi, mkt, mktB2c, mktB2b, tm);
        await context.SaveChangesAsync();

        var lcp = new User { FirstName = "Ioana", LastName = "Popescu", Email = "lcp@iasi.aiesec.ro" };
        var vpMkt = new User { FirstName = "Alex", LastName = "Vasile", Email = "vp.mkt@iasi.aiesec.ro" };
        var tlB2c = new User { FirstName = "Maria", LastName = "Ionescu", Email = "tl.b2c@iasi.aiesec.ro" };
        var mem1 = new User { FirstName = "Andrei", LastName = "Radu", Email = "andrei@iasi.aiesec.ro" };
        var mem2 = new User { FirstName = "Elena", LastName = "Stan", Email = "elena@iasi.aiesec.ro" };

        context.Users.AddRange(lcp, vpMkt, tlB2c, mem1, mem2);
        await context.SaveChangesAsync();

        var roleNatPres = await context.Roles.FirstAsync(r => r.Name == "NationalPresident");
        var roleLCP = await context.Roles.FirstAsync(r => r.Name == "LocalPresident");
        var roleVP = await context.Roles.FirstAsync(r => r.Name == "LocalVicePresident");
        var roleTL = await context.Roles.FirstAsync(r => r.Name == "TeamLeader");
        var roleMem = await context.Roles.FirstAsync(r => r.Name == "Member");

        context.UserUnitRoles.AddRange(
            new UserUnitRole { UserId = admin.Id, OrganizationUnitId = romania.Id, RoleId = roleNatPres.Id },
            new UserUnitRole { UserId = lcp.Id, OrganizationUnitId = iasi.Id, RoleId = roleLCP.Id },
            new UserUnitRole { UserId = vpMkt.Id, OrganizationUnitId = mkt.Id, RoleId = roleVP.Id },
            new UserUnitRole { UserId = tlB2c.Id, OrganizationUnitId = mktB2c.Id, RoleId = roleTL.Id },
            new UserUnitRole { UserId = mem1.Id, OrganizationUnitId = mktB2c.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = mem2.Id, OrganizationUnitId = mktB2c.Id, RoleId = roleMem.Id }
        );
        await context.SaveChangesAsync();

        context.Tasks.AddRange(
            new TaskItem { Title = "Plan Promotion Campaign", Description = "Create content for IG", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = mktB2c.Id, AssigneeId = mem1.Id, CreatorId = tlB2c.Id },
            new TaskItem { Title = "Design Posters", Description = "Canva templates", Priority = TaskPriority.Medium, Status = TaskStatus.Done, OrganizationUnitId = mktB2c.Id, AssigneeId = mem2.Id, CreatorId = tlB2c.Id },
            new TaskItem { Title = "Weekly Sync", Description = "MKT sync", Priority = TaskPriority.Low, Status = TaskStatus.ToDo, OrganizationUnitId = mkt.Id, AssigneeId = vpMkt.Id, CreatorId = vpMkt.Id }
        );
        
        var ev = new Event { Title = "Local Committee Meeting", Description = "LCM Iasi", StartDate = DateTime.UtcNow.AddDays(2), EndDate = DateTime.UtcNow.AddDays(2).AddHours(2), OrganizationUnitId = iasi.Id };
        context.Events.Add(ev);
        await context.SaveChangesAsync();

        context.ActivityLogs.AddRange(
            new ActivityLog { UserId = mem2.Id, OrganizationUnitId = mktB2c.Id, Action = "TaskDone", EntityType = "Task", Details = "Completed Design Posters" },
            new ActivityLog { UserId = mem1.Id, OrganizationUnitId = mktB2c.Id, Action = "TaskDone", EntityType = "Task", Details = "Past task" },
            new ActivityLog { UserId = mem1.Id, OrganizationUnitId = mktB2c.Id, Action = "TaskDone", EntityType = "Task", Details = "Past task 2" },
            new ActivityLog { UserId = vpMkt.Id, OrganizationUnitId = mkt.Id, Action = "EventCreated", EntityType = "Event", Details = "Marketing Sync" },
            new ActivityLog { UserId = tlB2c.Id, OrganizationUnitId = mktB2c.Id, Action = "TaskDone", EntityType = "Task", Details = "Planning" }
        );

        await context.SaveChangesAsync();
    }
}
