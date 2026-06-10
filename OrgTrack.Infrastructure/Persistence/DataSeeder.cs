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
        if (await context.OrganizationUnits.AnyAsync())
        {
            return;
        }

        var adminEmail = "lungufabian718@gmail.com";
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        
        if (context.Database.IsRelational())
        {
            await context.ActivityLogs.ExecuteDeleteAsync();
            await context.EventRsvps.ExecuteDeleteAsync();
            await context.EventInvitedUnits.ExecuteDeleteAsync();
            await context.EventInvitedUsers.ExecuteDeleteAsync();
            await context.Events.ExecuteDeleteAsync();
            await context.Tasks.ExecuteDeleteAsync();
            await context.InviteLinks.ExecuteDeleteAsync();
            await context.UserUnitRoles.ExecuteDeleteAsync();
            await context.Messages.ExecuteDeleteAsync();
            await context.Notifications.ExecuteDeleteAsync();
            await context.RefreshTokens.ExecuteDeleteAsync();
            await context.OrganizationUnits.ExecuteDeleteAsync();
            await context.Users.Where(u => u.Email != adminEmail).ExecuteDeleteAsync();
        }

        if (admin == null)
        {
            admin = new User { FirstName = "Fabian", LastName = "Lungu", Email = adminEmail, IsActive = true };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }

        // UNITAȚI
        var romania = new OrganizationUnit { Name = "AIESEC in Romania", Type = UnitType.National, Description = "National Member Committee" };
        var iasi = new OrganizationUnit { Name = "AIESEC in Iasi", Type = UnitType.Committee, ParentUnit = romania, Description = "Local Committee Iasi" };
        var bucharest = new OrganizationUnit { Name = "AIESEC in Bucharest", Type = UnitType.Committee, ParentUnit = romania, Description = "Local Committee Bucharest" };
        var cluj = new OrganizationUnit { Name = "AIESEC in Cluj-Napoca", Type = UnitType.Committee, ParentUnit = romania, Description = "Local Committee Cluj" };
        
        // Iasi Departments & Teams
        var mktIasi = new OrganizationUnit { Name = "Marketing Iasi", Type = UnitType.Department, DepartmentType = DepartmentType.Marketing, ParentUnit = iasi };
        var b2cIasi = new OrganizationUnit { Name = "B2C MKT Iasi", Type = UnitType.Team, ParentUnit = mktIasi };
        var b2bIasi = new OrganizationUnit { Name = "B2B MKT Iasi", Type = UnitType.Team, ParentUnit = mktIasi };
        var tmIasi = new OrganizationUnit { Name = "Talent Management Iasi", Type = UnitType.Department, DepartmentType = DepartmentType.HR, ParentUnit = iasi };
        var recTmIasi = new OrganizationUnit { Name = "Recruitment Iasi", Type = UnitType.Team, ParentUnit = tmIasi };
        var mxTmIasi = new OrganizationUnit { Name = "Members Experience Iasi", Type = UnitType.Team, ParentUnit = tmIasi };
        var ogxIasi = new OrganizationUnit { Name = "Outgoing Exchange Iasi", Type = UnitType.Department, DepartmentType = DepartmentType.Sales, ParentUnit = iasi };

        // Bucharest Departments & Teams
        var finBucharest = new OrganizationUnit { Name = "Finance Bucharest", Type = UnitType.Department, DepartmentType = DepartmentType.Finance, ParentUnit = bucharest };
        var accFinBucharest = new OrganizationUnit { Name = "Accounting Bucharest", Type = UnitType.Team, ParentUnit = finBucharest };
        var audFinBucharest = new OrganizationUnit { Name = "Auditing Bucharest", Type = UnitType.Team, ParentUnit = finBucharest };
        var mktBucharest = new OrganizationUnit { Name = "Marketing Bucharest", Type = UnitType.Department, DepartmentType = DepartmentType.Marketing, ParentUnit = bucharest };
        var tmBucharest = new OrganizationUnit { Name = "HR Bucharest", Type = UnitType.Department, DepartmentType = DepartmentType.HR, ParentUnit = bucharest };

        // Cluj Departments
        var itCluj = new OrganizationUnit { Name = "IT Cluj", Type = UnitType.Department, DepartmentType = DepartmentType.IT, ParentUnit = cluj };
        var mktCluj = new OrganizationUnit { Name = "Marketing Cluj", Type = UnitType.Department, DepartmentType = DepartmentType.Marketing, ParentUnit = cluj };

        context.OrganizationUnits.AddRange(romania, iasi, bucharest, cluj, mktIasi, b2cIasi, b2bIasi, tmIasi, recTmIasi, mxTmIasi, ogxIasi, finBucharest, accFinBucharest, audFinBucharest, mktBucharest, tmBucharest, itCluj, mktCluj);
        await context.SaveChangesAsync();

        // USERS
        // Iasi
        var lcpIasi = new User { FirstName = "Ioana", LastName = "Popescu", Email = "lcp@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Ioana" };
        var vpMktIasi = new User { FirstName = "Alex", LastName = "Vasile", Email = "vp.mkt@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Alex" };
        var tlB2cIasi = new User { FirstName = "Maria", LastName = "Ionescu", Email = "tl.b2c@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Maria" };
        var tlB2bIasi = new User { FirstName = "George", LastName = "Mihai", Email = "tl.b2b@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=George" };
        var memB2c1 = new User { FirstName = "Andrei", LastName = "Radu", Email = "andrei@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Andrei" };
        var memB2c2 = new User { FirstName = "Elena", LastName = "Stan", Email = "elena@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Elena" };
        var memB2c3 = new User { FirstName = "Bogdan", LastName = "Iliescu", Email = "bogdan@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Bogdan" };
        var memB2c4 = new User { FirstName = "Carla", LastName = "Zamfir", Email = "carla@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Carla" };
        var memB2b1 = new User { FirstName = "Victor", LastName = "Dumitrescu", Email = "victor@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Victor" };
        var memB2b2 = new User { FirstName = "Denis", LastName = "Oprea", Email = "denis@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Denis" };
        var memB2b3 = new User { FirstName = "Emma", LastName = "Rusu", Email = "emma@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Emma" };
        
        var vpTmIasi = new User { FirstName = "Diana", LastName = "Marin", Email = "vp.tm@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Diana" };
        var tlRecIasi = new User { FirstName = "Silviu", LastName = "Popa", Email = "tl.rec@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Silviu" };
        var memRec1 = new User { FirstName = "Cristina", LastName = "Dima", Email = "cristina@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Cristina" };
        var memRec2 = new User { FirstName = "Florin", LastName = "Gavrilescu", Email = "florin@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Florin" };
        var memRec3 = new User { FirstName = "Gabi", LastName = "Neagu", Email = "gabi@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Gabi" };
        var memMx1 = new User { FirstName = "Paul", LastName = "Ilie", Email = "paul@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Paul" };
        var memMx2 = new User { FirstName = "Horia", LastName = "Nistor", Email = "horia@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Horia" };
        var memMx3 = new User { FirstName = "Iulia", LastName = "Marin", Email = "iulia@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Iulia" };

        var vpOgxIasi = new User { FirstName = "Ana", LastName = "Vlad", Email = "vp.ogx@iasi.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Ana" };

        // Bucharest
        var lcpBucharest = new User { FirstName = "Mihai", LastName = "Gheorghe", Email = "lcp@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Mihai" };
        var vpFinBucharest = new User { FirstName = "Laura", LastName = "Dobre", Email = "vp.fin@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Laura" };
        var tlAccBucharest = new User { FirstName = "Radu", LastName = "Ciobanu", Email = "tl.acc@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Radu" };
        var memAcc1 = new User { FirstName = "Simona", LastName = "Badea", Email = "simona@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Simona" };
        var memAcc2 = new User { FirstName = "Kevin", LastName = "Dobre", Email = "kevin@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Kevin" };
        var memAud1 = new User { FirstName = "Tudor", LastName = "Lazar", Email = "tudor@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Tudor" };
        var memAud2 = new User { FirstName = "Larisa", LastName = "Ene", Email = "larisa@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Larisa" };

        var vpMktBucharest = new User { FirstName = "Oana", LastName = "Sava", Email = "vp.mkt@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Oana" };
        var vpTmBucharest = new User { FirstName = "Carmen", LastName = "Pop", Email = "vp.tm@bucharest.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Carmen" };

        // Cluj
        var lcpCluj = new User { FirstName = "David", LastName = "Munteanu", Email = "lcp@cluj.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=David" };
        var vpItCluj = new User { FirstName = "Alexandru", LastName = "Toma", Email = "vp.it@cluj.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Alexandru" };
        var vpMktCluj = new User { FirstName = "Sergiu", LastName = "Balan", Email = "vp.mkt@cluj.aiesec.ro", PictureUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=Sergiu" };

        context.Users.AddRange(lcpIasi, vpMktIasi, tlB2cIasi, tlB2bIasi, memB2c1, memB2c2, memB2c3, memB2c4, memB2b1, memB2b2, memB2b3, vpTmIasi, tlRecIasi, memRec1, memRec2, memRec3, memMx1, memMx2, memMx3, vpOgxIasi, lcpBucharest, vpFinBucharest, tlAccBucharest, memAcc1, memAcc2, memAud1, memAud2, vpMktBucharest, vpTmBucharest, lcpCluj, vpItCluj, vpMktCluj);
        await context.SaveChangesAsync();

        // ROLES
        var roleNatPres = await context.Roles.FirstAsync(r => r.Name == "NationalPresident");
        var roleLCP = await context.Roles.FirstAsync(r => r.Name == "LocalPresident");
        var roleVP = await context.Roles.FirstAsync(r => r.Name == "LocalVicePresident");
        var roleTL = await context.Roles.FirstAsync(r => r.Name == "TeamLeader");
        var roleMem = await context.Roles.FirstAsync(r => r.Name == "Member");

        context.UserUnitRoles.AddRange(
            new UserUnitRole { UserId = admin.Id, OrganizationUnitId = romania.Id, RoleId = roleNatPres.Id },
            
            // Iasi
            new UserUnitRole { UserId = lcpIasi.Id, OrganizationUnitId = iasi.Id, RoleId = roleLCP.Id },
            new UserUnitRole { UserId = vpMktIasi.Id, OrganizationUnitId = mktIasi.Id, RoleId = roleVP.Id },
            new UserUnitRole { UserId = tlB2cIasi.Id, OrganizationUnitId = b2cIasi.Id, RoleId = roleTL.Id },
            new UserUnitRole { UserId = tlB2bIasi.Id, OrganizationUnitId = b2bIasi.Id, RoleId = roleTL.Id },
            new UserUnitRole { UserId = memB2c1.Id, OrganizationUnitId = b2cIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memB2c2.Id, OrganizationUnitId = b2cIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memB2c3.Id, OrganizationUnitId = b2cIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memB2c4.Id, OrganizationUnitId = b2cIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memB2b1.Id, OrganizationUnitId = b2bIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memB2b2.Id, OrganizationUnitId = b2bIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memB2b3.Id, OrganizationUnitId = b2bIasi.Id, RoleId = roleMem.Id },
            
            new UserUnitRole { UserId = vpTmIasi.Id, OrganizationUnitId = tmIasi.Id, RoleId = roleVP.Id },
            new UserUnitRole { UserId = tlRecIasi.Id, OrganizationUnitId = recTmIasi.Id, RoleId = roleTL.Id },
            new UserUnitRole { UserId = memRec1.Id, OrganizationUnitId = recTmIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memRec2.Id, OrganizationUnitId = recTmIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memRec3.Id, OrganizationUnitId = recTmIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memMx1.Id, OrganizationUnitId = mxTmIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memMx2.Id, OrganizationUnitId = mxTmIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memMx3.Id, OrganizationUnitId = mxTmIasi.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = vpOgxIasi.Id, OrganizationUnitId = ogxIasi.Id, RoleId = roleVP.Id },

            // Bucharest
            new UserUnitRole { UserId = lcpBucharest.Id, OrganizationUnitId = bucharest.Id, RoleId = roleLCP.Id },
            new UserUnitRole { UserId = vpFinBucharest.Id, OrganizationUnitId = finBucharest.Id, RoleId = roleVP.Id },
            new UserUnitRole { UserId = tlAccBucharest.Id, OrganizationUnitId = accFinBucharest.Id, RoleId = roleTL.Id },
            new UserUnitRole { UserId = memAcc1.Id, OrganizationUnitId = accFinBucharest.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memAcc2.Id, OrganizationUnitId = accFinBucharest.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memAud1.Id, OrganizationUnitId = audFinBucharest.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = memAud2.Id, OrganizationUnitId = audFinBucharest.Id, RoleId = roleMem.Id },
            new UserUnitRole { UserId = vpMktBucharest.Id, OrganizationUnitId = mktBucharest.Id, RoleId = roleVP.Id },
            new UserUnitRole { UserId = vpTmBucharest.Id, OrganizationUnitId = tmBucharest.Id, RoleId = roleVP.Id },

            // Cluj
            new UserUnitRole { UserId = lcpCluj.Id, OrganizationUnitId = cluj.Id, RoleId = roleLCP.Id },
            new UserUnitRole { UserId = vpItCluj.Id, OrganizationUnitId = itCluj.Id, RoleId = roleVP.Id },
            new UserUnitRole { UserId = vpMktCluj.Id, OrganizationUnitId = mktCluj.Id, RoleId = roleVP.Id }
        );
        await context.SaveChangesAsync();

        // TASKS
        context.Tasks.AddRange(
            new TaskItem { Title = "Plan B2C Instagram Campaign", Description = "Create content for IG. Focus on student recruitment.", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c1.Id, CreatorId = tlB2cIasi.Id },
            new TaskItem { Title = "Design Posters", Description = "Canva templates for the upcoming event.", Priority = TaskPriority.Medium, Status = TaskStatus.Done, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c2.Id, CreatorId = tlB2cIasi.Id },
            new TaskItem { Title = "TikTok Trends Research", Description = "Find trending audios for this week.", Priority = TaskPriority.Low, Status = TaskStatus.ToDo, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c3.Id, CreatorId = tlB2cIasi.Id },
            new TaskItem { Title = "Write Newsletter", Description = "Monthly newsletter to alumni.", Priority = TaskPriority.Medium, Status = TaskStatus.InProgress, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c4.Id, CreatorId = tlB2cIasi.Id },
            
            new TaskItem { Title = "Contact Local Partners", Description = "Pitch our new product to 5 local NGOs.", Priority = TaskPriority.High, Status = TaskStatus.ToDo, OrganizationUnitId = b2bIasi.Id, AssigneeId = memB2b1.Id, CreatorId = tlB2bIasi.Id },
            new TaskItem { Title = "Sponsorship Deck", Description = "Update the PDF presentation for companies.", Priority = TaskPriority.High, Status = TaskStatus.Done, OrganizationUnitId = b2bIasi.Id, AssigneeId = memB2b2.Id, CreatorId = tlB2bIasi.Id },
            new TaskItem { Title = "Call IT Companies", Description = "We need tech partners for the hackathon.", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = b2bIasi.Id, AssigneeId = memB2b3.Id, CreatorId = tlB2bIasi.Id },
            
            new TaskItem { Title = "Weekly Sync MKT", Description = "Marketing sync with all members.", Priority = TaskPriority.Low, Status = TaskStatus.ToDo, OrganizationUnitId = mktIasi.Id, AssigneeId = vpMktIasi.Id, CreatorId = vpMktIasi.Id },
            new TaskItem { Title = "Recruitment Strategy", Description = "Plan HR campaign for Autumn.", Priority = TaskPriority.High, Status = TaskStatus.ToDo, OrganizationUnitId = tmIasi.Id, AssigneeId = vpTmIasi.Id, CreatorId = lcpIasi.Id },
            
            new TaskItem { Title = "Interviews setup", Description = "Contact applicants and set up calendars.", Priority = TaskPriority.Medium, Status = TaskStatus.InProgress, OrganizationUnitId = recTmIasi.Id, AssigneeId = memRec1.Id, CreatorId = tlRecIasi.Id },
            new TaskItem { Title = "Prepare Interview Questions", Description = "Update the standard questions.", Priority = TaskPriority.Medium, Status = TaskStatus.Done, OrganizationUnitId = recTmIasi.Id, AssigneeId = memRec2.Id, CreatorId = tlRecIasi.Id },
            new TaskItem { Title = "Book Rooms for Interviews", Description = "At the University.", Priority = TaskPriority.High, Status = TaskStatus.ToDo, OrganizationUnitId = recTmIasi.Id, AssigneeId = memRec3.Id, CreatorId = tlRecIasi.Id },
            
            new TaskItem { Title = "Members Survey", Description = "Send feedback form to members.", Priority = TaskPriority.Medium, Status = TaskStatus.Done, OrganizationUnitId = mxTmIasi.Id, AssigneeId = memMx1.Id, CreatorId = vpTmIasi.Id },
            new TaskItem { Title = "Organize Team Building", Description = "Find a cabin in the mountains.", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = mxTmIasi.Id, AssigneeId = memMx2.Id, CreatorId = vpTmIasi.Id },
            new TaskItem { Title = "Buy Snacks for Meeting", Description = "Chips and soda.", Priority = TaskPriority.Low, Status = TaskStatus.ToDo, OrganizationUnitId = mxTmIasi.Id, AssigneeId = memMx3.Id, CreatorId = vpTmIasi.Id },

            new TaskItem { Title = "Budget review Q3", Description = "Analyze expenditures for last quarter.", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = finBucharest.Id, AssigneeId = vpFinBucharest.Id, CreatorId = lcpBucharest.Id },
            new TaskItem { Title = "Submit Invoices", Description = "Register new invoices in accounting.", Priority = TaskPriority.Medium, Status = TaskStatus.ToDo, OrganizationUnitId = accFinBucharest.Id, AssigneeId = memAcc1.Id, CreatorId = tlAccBucharest.Id },
            new TaskItem { Title = "Check Bank Statements", Description = "Reconcile accounts for July.", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = accFinBucharest.Id, AssigneeId = memAcc2.Id, CreatorId = tlAccBucharest.Id },
            new TaskItem { Title = "Internal Audit", Description = "Audit the MKT expenses.", Priority = TaskPriority.Medium, Status = TaskStatus.ToDo, OrganizationUnitId = audFinBucharest.Id, AssigneeId = memAud1.Id, CreatorId = vpFinBucharest.Id },
            new TaskItem { Title = "Legal Compliance Check", Description = "Verify GDPR documents.", Priority = TaskPriority.High, Status = TaskStatus.Done, OrganizationUnitId = audFinBucharest.Id, AssigneeId = memAud2.Id, CreatorId = vpFinBucharest.Id },
            
            new TaskItem { Title = "National Report", Description = "Consolidate national metrics for global.", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = romania.Id, AssigneeId = admin.Id, CreatorId = admin.Id },
            new TaskItem { Title = "Setup Server Infrastructure", Description = "Deploy docker containers.", Priority = TaskPriority.High, Status = TaskStatus.Done, OrganizationUnitId = itCluj.Id, AssigneeId = vpItCluj.Id, CreatorId = lcpCluj.Id },

            // BURNOUT TARGET: Andrei Radu (memB2c1)
            // Baseline tasks (fast completion)
            new TaskItem { Title = "Old Task 1", Description = "Baseline", Priority = TaskPriority.Low, Status = TaskStatus.Done, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c1.Id, CreatorId = tlB2cIasi.Id, CreatedAt = DateTime.UtcNow.AddDays(-40), UpdatedAt = DateTime.UtcNow.AddDays(-39) },
            new TaskItem { Title = "Old Task 2", Description = "Baseline", Priority = TaskPriority.Medium, Status = TaskStatus.Done, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c1.Id, CreatorId = tlB2cIasi.Id, CreatedAt = DateTime.UtcNow.AddDays(-35), UpdatedAt = DateTime.UtcNow.AddDays(-34) },
            // Recent slow tasks
            new TaskItem { Title = "Burnout Task 1", Description = "Late & Slow", Priority = TaskPriority.High, Status = TaskStatus.Done, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c1.Id, CreatorId = tlB2cIasi.Id, CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-2), Deadline = DateTime.UtcNow.AddDays(-5) },
            new TaskItem { Title = "Burnout Task 2", Description = "Late", Priority = TaskPriority.High, Status = TaskStatus.InProgress, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c1.Id, CreatorId = tlB2cIasi.Id, CreatedAt = DateTime.UtcNow.AddDays(-5), Deadline = DateTime.UtcNow.AddDays(-1) },
            new TaskItem { Title = "Burnout Task 3", Description = "Late", Priority = TaskPriority.High, Status = TaskStatus.ToDo, OrganizationUnitId = b2cIasi.Id, AssigneeId = memB2c1.Id, CreatorId = tlB2cIasi.Id, CreatedAt = DateTime.UtcNow.AddDays(-5), Deadline = DateTime.UtcNow.AddDays(-1) }
        );
        
        // EVENTS
        var ev0 = new Event { Title = "National Onboarding (Past)", Description = "Welcome all new members.", StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(-28), OrganizationUnitId = romania.Id };
        ev0.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = romania.Id });

        var ev1 = new Event { Title = "Local Committee Meeting Iasi", Description = "LCM Iasi for all members.", StartDate = DateTime.UtcNow.AddDays(2), EndDate = DateTime.UtcNow.AddDays(2).AddHours(2), OrganizationUnitId = iasi.Id };
        ev1.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = iasi.Id });

        var ev2 = new Event { Title = "National Conference", Description = "All LCs meeting in person.", StartDate = DateTime.UtcNow.AddDays(15), EndDate = DateTime.UtcNow.AddDays(17), OrganizationUnitId = romania.Id };
        ev2.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = romania.Id }); 

        var ev3 = new Event { Title = "Marketing Hackathon", Description = "MKT Strategy Day for new members.", StartDate = DateTime.UtcNow.AddDays(5), EndDate = DateTime.UtcNow.AddDays(5).AddHours(4), OrganizationUnitId = mktIasi.Id };
        ev3.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = mktIasi.Id }); 
        ev3.InvitedUsers.Add(new EventInvitedUser { UserId = admin.Id }); 

        var ev4 = new Event { Title = "Bucharest LCM", Description = "Monthly update in Bucharest.", StartDate = DateTime.UtcNow.AddDays(3), EndDate = DateTime.UtcNow.AddDays(3).AddHours(2), OrganizationUnitId = bucharest.Id };
        ev4.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = bucharest.Id });

        var ev5 = new Event { Title = "Finance Summit", Description = "National Finance strategy.", StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(11), OrganizationUnitId = finBucharest.Id };
        ev5.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = finBucharest.Id });
        ev5.InvitedUsers.Add(new EventInvitedUser { UserId = admin.Id }); 

        var ev6 = new Event { Title = "Global Volunteer Fair", Description = "Recruit students for exchange.", StartDate = DateTime.UtcNow.AddDays(20), EndDate = DateTime.UtcNow.AddDays(21), OrganizationUnitId = ogxIasi.Id };
        ev6.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = iasi.Id }); 
        ev6.InvitedUsers.Add(new EventInvitedUser { UserId = admin.Id }); 

        // BURNOUT TARGET EVENTS (Absenteeism Streak)
        var evPast1 = new Event { Title = "B2C Team Sync", Description = "Weekly", StartDate = DateTime.UtcNow.AddDays(-14), EndDate = DateTime.UtcNow.AddDays(-14).AddHours(1), OrganizationUnitId = b2cIasi.Id };
        var evPast2 = new Event { Title = "MKT Strategy", Description = "Monthly", StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow.AddDays(-7).AddHours(2), OrganizationUnitId = mktIasi.Id };
        var evPast3 = new Event { Title = "Emergency Meeting", Description = "Urgent", StartDate = DateTime.UtcNow.AddDays(-2), EndDate = DateTime.UtcNow.AddDays(-2).AddHours(1), OrganizationUnitId = b2cIasi.Id };
        
        evPast1.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = b2cIasi.Id });
        evPast2.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = mktIasi.Id });
        evPast3.InvitedUnits.Add(new EventInvitedUnit { OrganizationUnitId = b2cIasi.Id });

        context.Events.AddRange(ev0, ev1, ev2, ev3, ev4, ev5, ev6, evPast1, evPast2, evPast3);
        await context.SaveChangesAsync();

        // RSVPS
        context.EventRsvps.AddRange(
            new EventRsvp { EventId = ev0.Id, UserId = admin.Id, Rsvp = RsvpStatus.Going, Attendance = AttendanceStatus.Present },
            new EventRsvp { EventId = ev2.Id, UserId = admin.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev3.Id, UserId = admin.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev5.Id, UserId = admin.Id, Rsvp = RsvpStatus.Maybe },
            
            new EventRsvp { EventId = ev1.Id, UserId = lcpIasi.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev1.Id, UserId = vpMktIasi.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev1.Id, UserId = memB2c1.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev1.Id, UserId = memB2c2.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev1.Id, UserId = memB2c3.Id, Rsvp = RsvpStatus.Maybe },

            new EventRsvp { EventId = ev2.Id, UserId = lcpIasi.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev2.Id, UserId = lcpBucharest.Id, Rsvp = RsvpStatus.Going },
            new EventRsvp { EventId = ev2.Id, UserId = lcpCluj.Id, Rsvp = RsvpStatus.Going },

            // BURNOUT TARGET RSVPS
            new EventRsvp { EventId = evPast1.Id, UserId = memB2c1.Id, Rsvp = RsvpStatus.Going, Attendance = AttendanceStatus.Absent },
            new EventRsvp { EventId = evPast2.Id, UserId = memB2c1.Id, Rsvp = RsvpStatus.Going, Attendance = AttendanceStatus.Absent },
            new EventRsvp { EventId = evPast3.Id, UserId = memB2c1.Id, Rsvp = RsvpStatus.Going, Attendance = AttendanceStatus.Absent }
        );
        await context.SaveChangesAsync();

        // ACTIVITY LOGS
        context.ActivityLogs.AddRange(
            new ActivityLog { UserId = memB2c2.Id, OrganizationUnitId = b2cIasi.Id, Action = "TaskDone", EntityType = "Task", Details = "Completed Design Posters" },
            new ActivityLog { UserId = memB2c1.Id, OrganizationUnitId = b2cIasi.Id, Action = "TaskCreated", EntityType = "Task", Details = "Created Instagram Campaign" },
            new ActivityLog { UserId = vpMktIasi.Id, OrganizationUnitId = mktIasi.Id, Action = "EventCreated", EntityType = "Event", Details = "Marketing Hackathon event created" },
            new ActivityLog { UserId = memB2c3.Id, OrganizationUnitId = b2cIasi.Id, Action = "UserJoined", EntityType = "User", Details = "Joined B2C MKT Team" },
            new ActivityLog { UserId = memB2c4.Id, OrganizationUnitId = b2cIasi.Id, Action = "UserJoined", EntityType = "User", Details = "Joined B2C MKT Team" },
            
            new ActivityLog { UserId = lcpBucharest.Id, OrganizationUnitId = bucharest.Id, Action = "TaskCreated", EntityType = "Task", Details = "Budget review assigned" },
            new ActivityLog { UserId = memAcc1.Id, OrganizationUnitId = accFinBucharest.Id, Action = "TaskInProgress", EntityType = "Task", Details = "Started submitting invoices" },
            new ActivityLog { UserId = memAud2.Id, OrganizationUnitId = audFinBucharest.Id, Action = "TaskDone", EntityType = "Task", Details = "Completed Legal Compliance Check" },
            
            new ActivityLog { UserId = memRec1.Id, OrganizationUnitId = recTmIasi.Id, Action = "UserJoined", EntityType = "User", Details = "Joined Recruitment team" },
            new ActivityLog { UserId = memRec2.Id, OrganizationUnitId = recTmIasi.Id, Action = "TaskDone", EntityType = "Task", Details = "Prepared Interview Questions" },
            new ActivityLog { UserId = memMx2.Id, OrganizationUnitId = mxTmIasi.Id, Action = "TaskInProgress", EntityType = "Task", Details = "Organizing Team Building" },
            
            new ActivityLog { UserId = admin.Id, OrganizationUnitId = romania.Id, Action = "TaskCreated", EntityType = "Task", Details = "Created National Report task" },
            new ActivityLog { UserId = admin.Id, OrganizationUnitId = romania.Id, Action = "EventCreated", EntityType = "Event", Details = "Scheduled National Conference" },
            
            new ActivityLog { UserId = vpItCluj.Id, OrganizationUnitId = itCluj.Id, Action = "TaskDone", EntityType = "Task", Details = "Setup server successfully" },
            new ActivityLog { UserId = lcpCluj.Id, OrganizationUnitId = cluj.Id, Action = "UserJoined", EntityType = "User", Details = "Elected as LCP" },

            // BURNOUT TARGET CHURN (Frustration Index)
            new ActivityLog { UserId = memB2c1.Id, OrganizationUnitId = b2cIasi.Id, Action = "Status changed:", EntityType = "Task", Details = "Burnout Task 2 changed to InProgress", CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new ActivityLog { UserId = memB2c1.Id, OrganizationUnitId = b2cIasi.Id, Action = "Status changed:", EntityType = "Task", Details = "Burnout Task 2 changed to ToDo", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new ActivityLog { UserId = memB2c1.Id, OrganizationUnitId = b2cIasi.Id, Action = "Status changed:", EntityType = "Task", Details = "Burnout Task 2 changed to InProgress", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new ActivityLog { UserId = memB2c1.Id, OrganizationUnitId = b2cIasi.Id, Action = "Status changed:", EntityType = "Task", Details = "Burnout Task 2 changed to ToDo", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new ActivityLog { UserId = memB2c1.Id, OrganizationUnitId = b2cIasi.Id, Action = "Status changed:", EntityType = "Task", Details = "Burnout Task 2 changed to InProgress", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // MESSAGES (Simulating some chat history)
        context.Messages.AddRange(
            new Message { SenderId = admin.Id, ReceiverId = lcpIasi.Id, Content = "Hey Ioana, are the Iasi numbers for Q2 ready?", SentAt = DateTime.UtcNow.AddDays(-2), IsRead = true },
            new Message { SenderId = lcpIasi.Id, ReceiverId = admin.Id, Content = "Hi Fabian! Yes, I will upload them on the dashboard today.", SentAt = DateTime.UtcNow.AddDays(-2).AddHours(1), IsRead = true },
            new Message { SenderId = lcpBucharest.Id, ReceiverId = admin.Id, Content = "Don't forget about the Finance Summit next week!", SentAt = DateTime.UtcNow.AddHours(-5), IsRead = false },
            new Message { SenderId = vpMktIasi.Id, ReceiverId = admin.Id, Content = "Can we get more budget for the hackathon?", SentAt = DateTime.UtcNow.AddMinutes(-30), IsRead = false }
        );
        await context.SaveChangesAsync();
    }
}
