using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Priviledge : BaseEntity
    {
        public static Priviledge Create(String moduleName, String action)
        {
            return new Priviledge()
            {
                ModuleName = moduleName,
                Action = action,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string ModuleName { get; set; }
        public string Action { get; set; }
        public ICollection<RolePriviledge> RolePermissions { get; set; }
    }
}
