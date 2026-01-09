using qguardbackend.Data.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class InstitutionResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
        //public InstitutionTypeModel Type { get; set; }
        public string Logo { get; set; }
        public string AdminName { get; set; }
        public string AdminEmail { get; set; }
        public string HostName { get; set; }
        public string SSORequired { get; set; }
        public string SsoURL { get; set; }
        public string SsoLogo { get; set; }
        public string SenderEmail { get; set; }
        public string DefaultLanguage { get; set; }
        public string PrimaryThemeColor { get; set; }
        public string SecondaryThemeColor { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class InstitutionStatusResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class InstitutionCreateModel
    {
        [Required(ErrorMessage = "Institution name is required!")]
        [MinLength(3, ErrorMessage = "Institution name must be at least 3 characters long.")]
        [MaxLength(100, ErrorMessage = "Institution name cannot exceed 100 characters.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Institution code is required!")]
        [MinLength(3, ErrorMessage = "Institution code must be at least 3 characters long.")]
        [MaxLength(30, ErrorMessage = "Institution code cannot exceed 30 characters.")]
        public string Code { get; set; }
        [Required(ErrorMessage = "Admin name is required!")]
        [MinLength(3, ErrorMessage = "Admin name must be at least 3 characters long.")]
        [MaxLength(100, ErrorMessage = "Admin name cannot exceed 100 characters.")]
        public string AdminName { get; set; }
        [Required(ErrorMessage = "Admin email is required!")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string AdminEmail { get; set; }
        [Required(ErrorMessage = "Host name is required!")]
        public string HostName { get; set; }
        public SSORequired SSORequired { get; set; }
        public InstitutionTypesEnum InstitutionType { get; set; }
        //[Required(ErrorMessage = "SSO URL is required!")]
        public string? SsoURL { get; set; }
        public IFormFile? SsoLogo { get; set; }
        public IFormFile InstitutionLogo { get; set; }
        public string SenderEmail { get; set; }
        public LanguageType DefaultLanguage { get; set; }
        [Required(ErrorMessage = "Primary theme color is required!")]
        public string PrimaryThemeColor { get; set; }
        [Required(ErrorMessage = "Secondary theme color is required!")]
        public string SecondaryThemeColor { get; set; }
    }

    public class InstitutionFilterModel : QueryModelMini
    {
        public bool? Status { get; set; }
    }

    public class ExamSchedulenFilterModel : QueryModelMini
    {
        public ExamScheduleStatusEnum? Status { get; set; }
        public long? SessionId { get; set; }
        public long? SemesterId { get; set; }
        public long? LevelId { get; set; }
    }
    public class ExamSchedulenDashboardFilterModel
    {
        public ExamTimingStatusEnum Timing { get; set; }
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
    }

    public class UsersFilterModel : QueryModelMini
    {
        public RolesEnum? Role { get; set; }
    }

    public class CandidateFilterModel : QueryModelMini
    {
        public string Name { get; set; }
        public string RegNo { get; set; }
        public long? LevelId { get; set; }
        public long? SessionId { get; set; }
        public long? SemesterId { get; set; }
        public long? DepartmentId { get; set; }
        public long? ProgramId { get; set; }
        public long? FacultyId { get; set; }
    }

    public class InstitutionTypeModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class InstitutionTypeCreateModel
    {
        [Required(ErrorMessage = "Institution type name is required!")]
        public string Name { get; set; }
    }

    public class InstitutionTypeFilterModel : QueryModelMini
    {
        public string Name { get; set; }
    }
}