using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Session : BaseEntity
    {
        public static Session Create(String name, long InstitutionId)
        {
            return new Session()
            {
                Name = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                InstitutionId = InstitutionId
            };
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}
