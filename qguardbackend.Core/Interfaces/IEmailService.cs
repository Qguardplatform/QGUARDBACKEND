using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.EmailDtos;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.BoilerPlate.Service.Interfaces
{
    public interface IEmailService
    {
        Task<bool> EmailConfirmation(string token, string email, string fullName, string userId, string url);
        Task<string> EmailConfirmations(string token, string email, string fullName, string userId, string url);
        Task<bool> ForgetPasswordEmail(string token, string email, string fullName, string userId, string url, string schoolLogo);
        bool SendMail(string subject, string email, string content, string fromName = null, string fromEmail = null, string[] cc = null);
        Task<bool> Notify(string email, string subject, string fullName, string message, string title);
        Task<bool> SendContactFormEmail(string email, ContactModel model);
        Task<CustomResult<bool>> SendSupportMail(SendSupportEmailVM model);
        Task<CustomResult<bool>> SendContactUsEmail(SendContactUsEmailVM model);
        Task<CustomResult<bool>> SendWelcomeEmailAsync(SendWelcomeEmailVM model);
        Task<CustomResult<bool>> SendNewDefaultPasswordEmailAsync(SendWelcomeEmailVM model);
        Task<CustomResult<bool>> SendOTPEmailAsync(SendOTPVerificationEmailVM model);
        Task<CustomResult<bool>> SendExamSubmissionEmailAsync(CustomSendExamDto model);
        Task<CustomResult<bool>> SendExamStartingEmailAsync(CustomSendExamDto model);
        Task<CustomResult<bool>> SendExamReminderEmailAsync(CustomSendExamDto model);
        Task<CustomResult<bool>> SendExamPublishedEmailAsync(CustomSendExamDto model);
        Task<CustomResult<bool>> SendExamGradingEmailAsync(CustomSendExamDto model);
        Task<CustomResult<bool>> SendPasswordResetEmailAsync(SendPasswordResetEmailVM model);
        Task<bool> UnverifiedAccountPasswordLink(string token, string email, string fullName, string userId, string url);
        EmailLog EmailConfirmationV2(string token, string email, string fullName, string userId, string url, string userType, string schoolLogo, string schoolName = null, string password = null);
    }
}