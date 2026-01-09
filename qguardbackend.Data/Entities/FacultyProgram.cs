using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class FacultyProgram : BaseEntity
    {
        public static FacultyProgram Create(long facultyId, long programId, long institutionId)
        {
            return new FacultyProgram()
            {
               
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                FacultyId = facultyId,
                ProgramId = programId,
                InstitutionId = institutionId
            };
        }

        public long? FacultyId { get; set; }
        public Faculty Faculty { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
        public long? ProgramId { get; set; }
        public Program Program { get; set; }
    }
}
