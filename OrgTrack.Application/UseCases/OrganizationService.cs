using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Domain.Enums;

namespace OrgTrack.Application.UseCases;

public class OrganizationService(
    IOrganizationUnitRepository unitRepository,
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    ActivityLogService activityLogService)
{
    /// <summary>
    /// Creates a new organizational unit (Committee, Department, or Team).
    /// Validates that:
    /// - The type is valid
    /// - If it has a parent, the parent exists
    /// - The hierarchy is respected (a Department cannot be the child of a Team)
    /// </summary>
    public async Task<OrganizationUnitDto> CreateUnitAsync(
        string name, string description, string type, string? departmentType, Guid? parentUnitId, Guid creatorUserId)
    {
        if (!Enum.TryParse<UnitType>(type, ignoreCase: true, out var unitType))
        {
            throw new ArgumentException($"Invalid type: '{type}'. Accepted values: National, Committee, Department, Team.");
        }
        var deptType = DepartmentType.None;
        if (unitType == UnitType.Department)
        {
            if (string.IsNullOrEmpty(departmentType))
            {
                throw new ArgumentException("DepartmentType is required for units of type Department.");
            }
            if (!Enum.TryParse<DepartmentType>(departmentType, ignoreCase: true, out deptType))
            {
                throw new ArgumentException($"Invalid DepartmentType: '{departmentType}'.");
            }
        }
        if (parentUnitId.HasValue)
        {
            var parent = await unitRepository.GetByIdAsync(parentUnitId.Value);
            if (parent == null)
            {
                throw new ArgumentException("Parent unit not found.");
            }
            ValidateHierarchy(parent.Type, unitType);
        }
        else if (unitType != UnitType.National)
        {
            throw new ArgumentException("Only units of type National can be root (without parent).");
        }

        var unit = new OrganizationUnit
        {
            Name = name,
            Description = description,
            Type = unitType,
            DepartmentType = deptType,
            ParentUnitId = parentUnitId
        };

        await unitRepository.AddAsync(unit);
        if (unitType == UnitType.National)
        {
            var role = await roleRepository.GetByNameAsync("NationalPresident");
            if (role != null)
            {
                await unitRepository.AddMemberAsync(new UserUnitRole
                {
                    UserId = creatorUserId,
                    OrganizationUnitId = unit.Id,
                    RoleId = role.Id
                });
            }
        }
        await activityLogService.LogUnitCreatedAsync(creatorUserId, unit.Id, unit.Name);

        return MapToDto(unit);
    }

    /// <summary>
    /// Returns a unit with its direct children and members.
    /// </summary>
    public async Task<OrganizationUnitDto?> GetUnitByIdAsync(Guid id)
    {
        var unit = await unitRepository.GetByIdWithChildrenAsync(id);
        return unit == null ? null : MapToDto(unit);
    }

    /// <summary>
    /// Returns the complete tree starting from the root nodes.
    /// It is built recursively: National → Committees → Departments → Teams.
    /// </summary>
    public async Task<List<OrganizationUnitDto>> GetFullTreeAsync()
    {
        var allUnits = await unitRepository.GetAllUnitsAsync();
        var roots = allUnits.Where(u => u.ParentUnitId == null).ToList();
        var result = new List<OrganizationUnitDto>();

        foreach (var root in roots)
        {
            result.Add(BuildTreeInMemory(root, allUnits));
        }

        return result;
    }

    /// <summary>
    /// Updates the name and description of a unit.
    /// </summary>
    public async Task<OrganizationUnitDto?> UpdateUnitAsync(Guid id, string name, string description)
    {
        var unit = await unitRepository.GetByIdAsync(id);
        if (unit == null) return null;

        unit.Name = name;
        unit.Description = description;
        unit.UpdatedAt = DateTime.UtcNow;

        await unitRepository.UpdateAsync(unit);

        return MapToDto(unit);
    }



    /// <summary>
    /// Deletes a unit from the tree.
    /// WARNING: Due to DeleteBehavior.Restrict on the parent-child relationship,
    /// all child units must be deleted first.
    /// </summary>
    public async Task<bool> DeleteUnitAsync(Guid id)
    {
        var unit = await unitRepository.GetByIdWithChildrenAsync(id);
        if (unit == null) return false;

        if (unit.ChildUnits.Any())
        {
            throw new InvalidOperationException(
                "You cannot delete a unit that has sub-units. Delete the children first.");
        }

        await unitRepository.DeleteAsync(unit);
        return true;
    }
    /// <summary>
    /// Assigns a user to a unit with a specific role.
    /// Top-Down Flow: President adds VP, VP adds TL.
    /// If the user does not exist in the system yet (has never logged in),
    /// we pre-create them with their email — when they log in with Google, the account will be ready.
    /// </summary>
    public async Task<UnitMemberDto> AssignMemberAsync(Guid unitId, string userEmail, string roleName, Guid actorUserId)
    {
        var unit = await unitRepository.GetByIdAsync(unitId);
        if (unit == null)
        {
            throw new ArgumentException("Organization unit not found.");
        }
        var role = await roleRepository.GetByNameAsync(roleName);
        if (role == null)
        {
            var allRoles = await roleRepository.GetAllAsync();
            var validNames = string.Join(", ", allRoles.Select(r => r.Name));
            throw new ArgumentException(
                $"Role '{roleName}' does not exist. Available roles: {validNames}");
        }
        var user = await userRepository.GetByEmailAsync(userEmail);
        if (user == null)
        {
            user = new User
            {
                Email = userEmail,
                FirstName = "Pending",
                LastName = "User",
                IsActive = true
            };
            await userRepository.AddAsync(user);
        }
        var existingRole = await unitRepository.GetUserUnitRoleAsync(user.Id, unitId);
        if (existingRole != null)
        {
            throw new InvalidOperationException(
                $"User {userEmail} already has a role in this unit.");
        }

        var userUnitRole = new UserUnitRole
        {
            UserId = user.Id,
            OrganizationUnitId = unitId,
            RoleId = role.Id
        };

        await unitRepository.AddMemberAsync(userUnitRole);
        await activityLogService.LogMemberJoinedAsync(actorUserId, user.Id, unitId);

        return new UnitMemberDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            role.Name, userUnitRole.CreatedAt, null, null, user.PictureUrl
        );
    }

    /// <summary>
    /// Returns all members of a unit with their roles.
    /// </summary>
    public async Task<List<UnitMemberDto>> GetMembersAsync(Guid unitId)
    {
        var unit = await unitRepository.GetByIdAsync(unitId);
        if (unit == null) return new List<UnitMemberDto>();

        List<UserUnitRole> members;

        if (unit.Type != UnitType.National)
        {
            var descendantIds = await unitRepository.GetDescendantUnitIdsAsync(unitId);
            var allIds = new List<Guid> { unitId };
            allIds.AddRange(descendantIds);
            members = await unitRepository.GetMembersForUnitsAsync(allIds);
            members = members.DistinctBy(m => m.UserId).ToList();
        }
        else
        {
            members = await unitRepository.GetMembersAsync(unitId);
        }

        return members.Select(m => new UnitMemberDto(
            m.UserId,
            m.User!.FirstName,
            m.User.LastName,
            m.User.Email,
            m.Role!.Name,
            m.CreatedAt,
            m.OrganizationUnit?.Name,
            m.OrganizationUnitId,
            m.User.PictureUrl
        )).ToList();
    }

    /// <summary>
    /// Removes a user from a unit.
    /// </summary>
    public async Task<bool> RemoveMemberAsync(Guid unitId, Guid userId)
    {
        var membership = await unitRepository.GetUserUnitRoleAsync(userId, unitId);
        if (membership == null) return false;

        await unitRepository.RemoveMemberAsync(membership);
        await activityLogService.LogMemberRemovedAsync(userId, userId, unitId);

        return true;
    }

    /// <summary>
    /// Returns the rank (power) of a role. 
    /// A user cannot assign a role with a higher rank than their maximum role.
    /// </summary>
    private int GetRoleRank(string roleName)
    {
        if (roleName.StartsWith("NationalPresident")) return 100;
        if (roleName.StartsWith("NationalVicePresident")) return 90;
        if (roleName == "LocalPresident") return 80;
        if (roleName.StartsWith("LocalVicePresident")) return 60;
        if (roleName == "TeamLeader") return 40;
        if (roleName == "Member") return 20;
        return 0;
    }

    /// <summary>
    /// Updates the role of an existing member in a unit. Optionally moves them to a different unit.
    /// </summary>
    public async Task<bool> UpdateMemberRoleAsync(Guid unitId, Guid targetUserId, string newRoleName, Guid actorUserId, Guid? targetUnitId = null)
    {
        var actorRoles = await unitRepository.GetUserRolesAsync(actorUserId);
        int actorMaxRank = actorRoles.Select(r => r.Role != null ? GetRoleRank(r.Role.Name) : 0).DefaultIfEmpty(0).Max();
        
        var actorUser = await userRepository.GetByIdAsync(actorUserId);
        if (actorUser != null && actorUser.Email == "admin@aiesec.ro")
            actorMaxRank = 1000; // Admin Backdoor
            
        int targetRoleRank = GetRoleRank(newRoleName);
        if (targetRoleRank > actorMaxRank)
        {
            throw new ArgumentException("Security Violation: You do not have a high enough role to assign this level of permission.");
        }

        var membership = await unitRepository.GetUserUnitRoleAsync(targetUserId, unitId);
        if (membership == null) return false;

        var role = await roleRepository.GetByNameAsync(newRoleName);
        if (role == null)
            throw new ArgumentException($"Role '{newRoleName}' does not exist.");
        var finalUnitId = targetUnitId ?? unitId;

        membership.RoleId = role.Id;
        
        if (finalUnitId != unitId)
        {
            var targetUnit = await unitRepository.GetByIdAsync(finalUnitId);
            if (targetUnit == null) throw new ArgumentException("Target unit does not exist.");
            var existingInTarget = await unitRepository.GetUserUnitRoleAsync(targetUserId, finalUnitId);
            if (existingInTarget != null) 
            {
                existingInTarget.RoleId = role.Id;
                await unitRepository.UpdateMemberAsync(existingInTarget);
                await unitRepository.RemoveMemberAsync(membership);
                return true;
            }

            membership.OrganizationUnitId = finalUnitId;
        }

        await unitRepository.UpdateMemberAsync(membership);

        return true;
    }
    /// <summary>
    /// Builds the recursive tree IN MEMORY, without making additional queries.
    /// Eliminates the N+1 Query problem.
    /// </summary>
    private OrganizationUnitDto BuildTreeInMemory(OrganizationUnit unit, List<OrganizationUnit> allUnits)
    {
        var childDtos = new List<OrganizationUnitDto>();
        var children = allUnits.Where(u => u.ParentUnitId == unit.Id).ToList();
        
        foreach (var child in children)
        {
            childDtos.Add(BuildTreeInMemory(child, allUnits));
        }

        var members = unit.Members.Select(m => new UnitMemberDto(
            m.UserId, m.User?.FirstName ?? "", m.User?.LastName ?? "",
            m.User?.Email ?? "", m.Role?.Name ?? "", m.CreatedAt, unit.Name, unit.Id, m.User?.PictureUrl
        )).ToList();

        foreach (var child in children)
        {
            if (child.Members != null)
            {
                var childLeaders = child.Members
                    .Where(m => m.Role != null && m.Role.Name != "Member")
                    .Select(m => new UnitMemberDto(
                        m.UserId, m.User?.FirstName ?? "", m.User?.LastName ?? "",
                        m.User?.Email ?? "", m.Role?.Name ?? "", m.CreatedAt, child.Name, child.Id, m.User?.PictureUrl
                    ));
                members.AddRange(childLeaders);
            }
        }

        return new OrganizationUnitDto(
            unit.Id, unit.Name, unit.Description,
            unit.Type.ToString(), unit.DepartmentType.ToString(),
            unit.ParentUnitId, unit.CreatedAt,
            childDtos,
            members
        );
    }

    /// <summary>
    /// Validates that the hierarchy is respected.
    /// National > Committee > Department > Team.
    /// A Committee cannot be the child of a Department, for example.
    /// </summary>
    private void ValidateHierarchy(UnitType parentType, UnitType childType)
    {
        var valid = (parentType, childType) switch
        {
            (UnitType.National, UnitType.Committee) => true,
            (UnitType.Committee, UnitType.Department) => true,
            (UnitType.Department, UnitType.Team) => true,
            _ => false
        };

        if (!valid)
        {
            throw new ArgumentException(
                $"Invalid hierarchy: a {childType} cannot be the child of a {parentType}. " +
                $"Correct order: National → Committee → Department → Team.");
        }
    }

    /// <summary>
    /// Converts an OrganizationUnit entity into a simple DTO (without recursion on children).
    /// </summary>
    private OrganizationUnitDto MapToDto(OrganizationUnit unit)
    {
        var members = unit.Members?.Select(m => new UnitMemberDto(
            m.UserId, m.User?.FirstName ?? "", m.User?.LastName ?? "",
            m.User?.Email ?? "", m.Role?.Name ?? "", m.CreatedAt, unit.Name, unit.Id, m.User?.PictureUrl
        )).ToList() ?? new List<UnitMemberDto>();
        if (unit.ChildUnits != null)
        {
            foreach (var child in unit.ChildUnits)
            {
                if (child.Members != null)
                {
                    var childLeaders = child.Members
                        .Where(m => m.Role != null && m.Role.Name != "Member") // Anyone who is not just a standard "Member"
                        .Select(m => new UnitMemberDto(
                            m.UserId, m.User?.FirstName ?? "", m.User?.LastName ?? "",
                            m.User?.Email ?? "", m.Role?.Name ?? "", m.CreatedAt, child.Name, child.Id, m.User?.PictureUrl
                        ));
                    members.AddRange(childLeaders);
                }
            }
        }

        return new OrganizationUnitDto(
            unit.Id, unit.Name, unit.Description,
            unit.Type.ToString(), unit.DepartmentType.ToString(),
            unit.ParentUnitId, unit.CreatedAt,
            unit.ChildUnits?.Select(c => MapToDto(c)).ToList() ?? new List<OrganizationUnitDto>(),
            members
        );
    }

    public async Task<IEnumerable<OrganizationUnitDto>> GetMyUnitsAsync(Guid userId)
    {
        var roles = await unitRepository.GetUserRolesAsync(userId);
        return roles
            .Where(r => r.OrganizationUnit != null)
            .Select(r => MapToDto(r.OrganizationUnit!));
    }
}
