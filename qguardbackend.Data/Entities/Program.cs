using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Program : BaseEntity
    {
        public static Program Create(String name, long institutionId)
        {
            return new Program()
            {
                Name = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                InstitutionId = institutionId
            };
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProgramType { get; set; }
        public long noOfSemesters { get; set; }
        public long DurationInMonths { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }


    }
}
