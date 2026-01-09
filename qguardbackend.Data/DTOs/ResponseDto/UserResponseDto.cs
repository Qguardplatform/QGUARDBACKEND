using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs.ResponseDto
{
    public class UserResponseDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string MatricNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string PicturePath { get; set; }
        public string otp { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime? AccountActivationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public List<string> Role { get; set; }
    }

    public class UserActivationRequestDto
    {
        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; }
    }

    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [MaxLength(60, ErrorMessage = "Email cannot be longer than 60 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        [MaxLength(16, ErrorMessage = "Password cannot be longer than 16 characters")]
        public string Password { get; set; }
    }
    public class ChangePasswordRequestDto
    {
        public string email { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
    public class PasswordResetRequestDto
    {
        public string email { get; set; }
        public string token { get; set; }
        public string NewPassword { get; set; }
    }

    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage ="Email Required")]
        public string email { get; set; }
 
    }
    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; }
        public string Email { get; set; }
    }

    public class ReturnTokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expires { get; set; }
        public bool Is2FAEnabled { get; set; }
        public UserResponseDto UserDetails { get; set; }
    }
    public class LoginResponse
    {
        public string Message { get; set; }
        public string UserId { get; set; }
        public string UId { get; set; }
        public long? TransporterId { get; set; }
        public long? DriverId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;

    }

    public class AuthClaims
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<string> Role { get; set; }
    }

    public class SendOTPResponseDto
    {
        public string UserId { get; set; }
        public string OTP { get; set; }
    }

    public class BuildNameModel
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
    }

    public class validateOTP
    {
        public string UserId { get; set; }
        public string otp { get; set; }
    }
}
