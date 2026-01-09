using Microsoft.AspNetCore.Http;

namespace qguardbackend.Data.DTOs.RequestDto
{
    public class CandidateRequestDto
    {
        //public long InstitutionId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; }
        public long LevelId { get; set; }
        public string MatricNumber { get; set; }
        public string RegistrationNumber { get; set; }

        public string Gender { get; set; }
        public IFormFile Passport { get; set; }
        public long ProgramId { get; set; }
        public long FacultyId { get; set; }
        public long DepartmentId { get; set; }
        public long SessionId { get; set; }
        public long SemesterId { get; set; }
        public Guid? CityId { get; set; }

        public Guid? RegionId { get; set; }

        public Guid? CountryId { get; set; }
    }


    public class UpdateCandidateStatusRequestDto
    {
        public List<long> CandidatesId { get; set; }
        public long? LevelId { get; set; }
        public long? SessionId { get; set; }
        public long? SemesterId { get; set; }
    }


    public class UpdateCandidateStatuResponseDto
    {
        public string Message { get; set; }
        public bool? Status { get; set; }
    }

    public class UpdateCandidateRequestDto
    {
        public long InstitutionId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public long LevelId { get; set; }
        public string MatricNumber { get; set; }
        public string Gender { get; set; }
        public IFormFile Passport { get; set; }
        public long ProgramId { get; set; }
        public long FacultyId { get; set; }
        public long DepartmentId { get; set; }
        public long SessionId { get; set; }
        public long SemesterId { get; set; }
        public Guid? CityId { get; set; }

        public Guid? RegionId { get; set; }

        public Guid? CountryId { get; set; }
    }
}
