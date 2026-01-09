using edutech.services.examportal.Data.Abstracts;

namespace edutech.services.examportal.Data.Entities
{
    public class InstitutionType : BaseEntity
    {
        public static InstitutionType Create(String name)
        {
            return new InstitutionType()
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
