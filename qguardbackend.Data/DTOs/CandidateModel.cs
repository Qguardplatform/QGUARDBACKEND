using Microsoft.AspNetCore.Http;

namespace qguardbackend.Data.DTOs
{
    public class CandidateModel
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string MatricNumber { get; set; }
        public string Gender { get; set; }
        public string PicturePath { get; set; }
        public InstitutionResponseDto Institution { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Level { get; set; }
        public ProgramModel Program { get; set; }
        public FacultyDto Faculty { get; set; }
        public DepartmentDto Department { get; set; }
        public SessionDto Session { get; set; }
        public SemesterModel Semester { get; set; }
        public LevelModel Levels { get; set; }
        public CityListDto City { get; set; }
        public RegionListDto State { get; set; }
        public CountryListDto Country { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CandidateListDto
    {
        public long Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string MatricNumber { get; set; }
        public string Email { get; set; }
    }

    public class CandidateUploadModel
    {
        public long LevelId { get; set; }
        public long SessionId { get; set; }
        public long SemesterId { get; set; }
        public IFormFile File { get; set; }
    }
    public class CandidatePreviewModel
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string MatricNumber { get; set; }
        public string Gender { get; set; }
        public string PicturePath { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsCandidate { get; set; }
        public bool IsFirstTimeLoginUser { get; set; }
        public long InstitutionId { get; set; }
        public string InstitutionName { get; set; }
    }
    public class CandidateExportModel
    {
        public long DepartmentId { get; set; }
        public long ProgramId { get; set; }
        public long FacultyId { get; set; }
        public long LevelId { get; set; }
        public long SessionId { get; set; }
        public long SemesterId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CandidateExportListModel
    {
        public string MatricNumber { get; set; }
        public string CandidateName { get; set; }
        public string Institution { get; set; }
        public string Semester { get; set; }
        public string Session { get; set; }
        public string Faculty { get; set; }
        public string Department { get; set; }
        public string Program { get; set; }
        public string Level { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Timestamp { get; set; }
    }
}
