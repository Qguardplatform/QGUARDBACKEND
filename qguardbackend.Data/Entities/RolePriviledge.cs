using qguardbackend.Data.Abstracts;
using qguardbackend.Data.Entities;

namespace qguardbackend.Data.Entities
{
    public class RolePriviledge : BaseEntity
    {
        public static RolePriviledge Create(String roleId, long priviledgeId)
        {
            return new RolePriviledge()
            {
                RoleId = roleId,
                PriviledgeId = priviledgeId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string RoleId { get; set; }
        public long PriviledgeId { get; set; }
        public ApplicationRole Role { get; set; }
        //public Priviledge Permission { get; set; }
    }
}
