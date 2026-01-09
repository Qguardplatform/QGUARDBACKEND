using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class EnforcementAction : BaseEntity
    {
        public static EnforcementAction Create(String name)
        {
            return new EnforcementAction()
            {
                Name = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string Name { get; set; }
    }
}