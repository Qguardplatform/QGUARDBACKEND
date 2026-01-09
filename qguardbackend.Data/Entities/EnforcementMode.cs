using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class EnforcementMode : BaseEntity
    {
        public static EnforcementMode Create(String name, String code, bool isActive)
        {
            return new EnforcementMode()
            {
                Name = name,
                Code = code,
                IsActive = isActive,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string Name { get; set; }
        public string Code { get; set; }
    }
}