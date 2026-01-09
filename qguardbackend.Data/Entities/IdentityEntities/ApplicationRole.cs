using qguardbackend.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace examportal.Data.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public ICollection<RolePriviledge> RolePermissions { get; set; }
    }
}