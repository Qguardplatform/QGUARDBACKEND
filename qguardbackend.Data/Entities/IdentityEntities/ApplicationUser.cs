using qguardbackend.Data.Entities;
using qguardbackend.Data.Model;
using Microsoft.AspNetCore.Identity;

namespace qguardbackend.Data.Entities;

public  class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime? AccountActivationDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
  
    public bool RequiresPasswordChange { get; set; }
    public string ActivationToken { get; set; }
    public DateTime? ActivationTokenExpiresOn { get; set; }

    public bool IsTokenActive { get; set; } = false;
    public string? OTP { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Gender { get; set; }

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
    public string ApprovalStatus { get; set; } = string.Empty;
    public string ApprovalActionBy { get; set; }
    public DateTime? ApprovalActionDate { get; set; }
  

    //public long InstitutionId { get; set; }
    //public Institution Institution { get; set; }

    public ICollection<ApplicationUserRole> UserRoles { get; set; } = new HashSet<ApplicationUserRole>();
}