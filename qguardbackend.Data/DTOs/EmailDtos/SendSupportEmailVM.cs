using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs.EmailDtos
{
    public class SendSupportEmailVM
    {
        [Required(ErrorMessage = "User id is required")]
        public Guid UserId { get; set; }
        [Required(ErrorMessage = "Email content is required")]
        public string EmailContent { get; set; }
    }
    public class ExamEmailQueueMessage
    {
        public string EmailType { get; set; }
    }
    public class SendWelcomeEmailVM
    {
        [Required(ErrorMessage = "Receiver email is required")]
        public string receiverEmail { get; set; }
        [Required(ErrorMessage = "Password is required")]

        public string Password { get; set; }
        public string InstitutionBaseUrl { get; set; }
        [Required(ErrorMessage = "Fullname is required")]
        public string Fullname { get; set; }
    }
    public class CustomSendExamDto
    {
        public string ExamTitle { get; set; }
        public string DateSubmitted { get; set; }
        public string CandidateName { get; set; }
        public string InstitutionBaseUrl { get; set; }
        public string CandidateEmail { get; set; }
        public string Grade { get; set; }
        public string Score { get; set; }
        public string Time { get; set; }
        public string Duration { get; set; }
        public string Date { get; set; }
    }

    public class SendOTPVerificationEmailVM
    {
        [Required(ErrorMessage = "Receiver email is required")]
        public string receiverEmail { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        public string OTP { get; set; }
        public string InstitutionBaseUrl { get; set; }
        
        [Required(ErrorMessage = "Fullname is required")]
        public string Fullname { get; set; }
    }
    public class SendPasswordResetEmailVM
    {
        [Required(ErrorMessage = "Receiver email is required")]
        public string receiverEmail { get; set; }
        public string PasswordResetToken { get; set; }
        public string InstitutionBaseUrl { get; set; }

        [Required(ErrorMessage = "Fullname is required")]
        public string Fullname { get; set; }
        public DateTime ExpiresOn { get; set; }
    }
}