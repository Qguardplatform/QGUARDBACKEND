namespace qguardbackend.Data.DTOs.ResponseDto
{
    public class ApplicationUserResponse
    {
        public string Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Gender { get; set; }
        public Guid RoleId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Country { get; set; }
        public string AuthProvider { get; set; }
        public string ProfilePixUrl { get; set; }

        public DateTime? LastSignInDate { get; set; }
        public bool IsActive { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string ApprovalActionBy { get; set; }
        public DateTime? ApprovalActionDate { get; set; }
        public DateTime DateCreated { get; set; }
        public List<ApplicationUserRoleResponse> Roles { get; set; }
        public InstitutionResponseDto Institution { get; set; }
    }

    public class ApplicationUserRoleResponse
    {
        public Guid Id { get; set; }

        public string Rolename { get; set; } = string.Empty;

    }
    public class ApplicationUserSignUpResponse
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; }
    }
    public class UserExportModel
    {
        public long InstitutionId { get; set; }
        public string RoleName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class UserExportListModel
    {
        public string FullName { get; set; }
        public string Institution { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Timestamp { get; set; }
    }
}
