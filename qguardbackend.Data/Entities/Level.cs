using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Level : BaseEntity
    {
        public static Level Create(String name, String desc, long institutionId)
        {
            return new Level()
            {
                LevelName = name,
                Description = desc,
                InstitutionId = institutionId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string LevelName { get; set; }
        public string Description { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}
