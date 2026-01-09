using qguardbackend.Core.Services;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.Core.Interfaces
{
    public interface IAuthService
    {
        Task<CustomResult<RegisterUserResponseDto>> RegisterAsync(RegisterUserRequestDto model, long institutionId);
        Task<CustomResult<RegisterUserResponseDto>> RegisterOtherUsersAsync(RegisterOtherUserRequestDto model);
        Task<CustomResult<RegisterUserResponseDto>> UpdateUserAsync(string userid, UpdateUserRequestDto model);
        Task<CustomResult<ReturnTokenModel>> LoginAsync(LoginRequestDto model);
        Task<CustomResult<ReturnTokenModel>> RefreshToken(RefreshTokenDto model);
         Task<CustomResult<bool>> ChangePasswordAsync(string email, string currentPassword, string newPassword);
        Task<CustomResult<string>> ForgotPasswordAsync(string email);
        Task<CustomResult<string>> SendNewLogInPassword(string email);
        Task<CustomResult<string>> SendNewLogInPasswordBulk(DefaultPasswordEmailsListDto requests);
        Task<CustomResult<string>> ResetPasswordAsync(string email, string token, string newPassword);
        Task ConfirmEmailAsync(string userId, string token);
        Task<string> GetRoleIdbyRoleName(string roleName);
        Task<CustomResult<bool>> AssignUserToRole(string userId, string rolename);
        Task<CustomResult<bool>> ValidateOtp(string userId, string Otp);
        Task<CustomResult<SendOTPResponseDto>> GenerateNewOTP(string userId);
        Task<CustomResult<SendOTPResponseDto>> ResendLoginAsync(string userid);
    }
}
