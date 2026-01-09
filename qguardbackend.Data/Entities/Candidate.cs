using qguardbackend.Data.Abstracts;
using examportal.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace qguardbackend.Data.Entities
{
    public class Candidate : BaseEntity
    {
        public static Candidate Create(String matricNo, String gender, String userId, String regNo, DateTime dob, String phone)
        {
            return new Candidate()
            {
                MatricNumber = matricNo,
                Gender = gender,
                UserId = userId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string MatricNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string Gender { get; set; }
        public string PicturePath { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
        public long? LevelId { get; set; }
        public Level Level { get; set; }
        public long? ProgramId { get; set; }
        public Program Program { get; set; }
        public long? FacultyId { get; set; }
        public Faculty Faculty { get; set; }
        public long? DepartmentId { get; set; }
        public Department Department { get; set; }
        public long? SessionId { get; set; }
        public Session Session { get; set; }
        public long? SemesterId { get; set; }
        public Semester Semester { get; set; }

        public Guid? CityId { get; set; }
        public City City { get; set; }

        public Guid? RegionId { get; set; }
        public Region Region { get; set; }

        public Guid? CountryId { get; set; }
        public Country Country { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
    }
}