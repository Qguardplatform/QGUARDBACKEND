using qguardbackend.Data.Abstracts;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace qguardbackend.Data.Model;

public class SystemAdminOtherTenantsRole : BaseEntity
{
    public long? InstitutionId { get; set; }

    //[ForeignKey(nameof(InstitutionId))]
    //public Institution Institution { get; set; }
    public string UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public ApplicationUser ApplicationUser { get; set; }
    public string? RoleId { get; set; }
    [ForeignKey(nameof(RoleId))]
    public ApplicationRole ApplicationRole { get; set; }
}



public class SystemAdminOtherTenantsRoleResponse
{
    public long? Id { get; set; }
    public long? InstitutionId { get; set; }
    public string? RoleName { get; set; }
    public DateTime? CreatedAt { get; set; }

    public string UserId { get; set; }

    public string? RoleId { get; set; }
}