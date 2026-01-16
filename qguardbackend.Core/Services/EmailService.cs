using Microsoft.Extensions.Options;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using qguardbackend.BoilerPlate.Service.Interfaces;
using qguardbackend.Core.ConfigModels;
using Microsoft.Extensions.Hosting;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DbContext;
using Microsoft.Extensions.Logging;
using qguardbackend.Data.DTOs.EmailDtos;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Constants;
using Newtonsoft.Json;
using qguardbackend.EmailTemplate;

namespace qguardbackend.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly IHostEnvironment _hostingEnvironment;
        private MailSetting _mailsettings;
        private readonly AppDbContext _dbcontext;
        private readonly AzureSetting _azureSetting;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IHostEnvironment hostingEnvironment,
            ILogger<EmailService> logger,
            IOptions<MailSetting> mailsettings,
            AppDbContext dbContext,
            IOptions<AzureSetting> azureSetting)
        {
            _hostingEnvironment = hostingEnvironment;
            _mailsettings = mailsettings.Value;
            _dbcontext = dbContext;
            _logger = logger;
            _azureSetting = azureSetting.Value;
        }

        public async Task<CustomResult<bool>> SendExamSubmissionEmailAsync(CustomSendExamDto model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ExamSubmissionEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{model.CandidateName}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##TITLE##", model.ExamTitle);
                fileContents = fileContents.Replace("##STUDENTEMAIL##", model.CandidateEmail);
                fileContents = fileContents.Replace("##DATE##", model.DateSubmitted);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.CandidateEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Exam Submission Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);

                return CustomResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<bool>> SendExamPublishedEmailAsync(CustomSendExamDto model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ExamPublishedEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{model.CandidateName}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##TITLE##", model.ExamTitle);
                fileContents = fileContents.Replace("##STUDENTEMAIL##", model.CandidateEmail);
                fileContents = fileContents.Replace("##DATE##", model.Date);
                fileContents = fileContents.Replace("##TIME##", model.Time);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.CandidateEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Exam Published Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);

                return CustomResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<bool>> SendExamGradingEmailAsync(CustomSendExamDto model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ExamGradingEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{model.CandidateName}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##TITLE##", model.ExamTitle);
                fileContents = fileContents.Replace("##STUDENTEMAIL##", model.CandidateEmail);
                fileContents = fileContents.Replace("##SCORE##", model.Score);
                fileContents = fileContents.Replace("##GRADE##", model.Grade);
                fileContents = fileContents.Replace("##URL##", model.InstitutionBaseUrl);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.CandidateEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Exam Grading Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);

                return CustomResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<bool>> SendExamStartingEmailAsync(CustomSendExamDto model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ExamStartingEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{model.CandidateName}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##TITLE##", model.ExamTitle);
                fileContents = fileContents.Replace("##STUDENTEMAIL##", model.CandidateEmail);
                fileContents = fileContents.Replace("##URL##", model.InstitutionBaseUrl);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.CandidateEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Exam Starting Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);

                return CustomResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<bool>> SendExamReminderEmailAsync(CustomSendExamDto model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ExamReminderEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{model.CandidateName}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##TITLE##", model.ExamTitle);
                fileContents = fileContents.Replace("##STUDENTEMAIL##", model.CandidateEmail);
                fileContents = fileContents.Replace("##DATE##", model.DateSubmitted);
                fileContents = fileContents.Replace("##TIME##", model.Time);
                fileContents = fileContents.Replace("##DURATION##", model.Duration);
                fileContents = fileContents.Replace("##URL##", model.InstitutionBaseUrl);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.CandidateEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Exam Reminder Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);

                return CustomResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<bool>> SendOTPEmailAsync(SendOTPVerificationEmailVM model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/OTPVerificationEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##EMAIL##", $"{model.receiverEmail}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##OTP##", model.OTP);

                fileContents = fileContents
                    .Replace("{{OTP1}}", model.OTP[0].ToString())
                    .Replace("{{OTP2}}", model.OTP[1].ToString())
                    .Replace("{{OTP3}}", model.OTP[2].ToString())
                    .Replace("{{OTP4}}", model.OTP[3].ToString())
                    .Replace("{{OTP5}}", model.OTP[4].ToString());
                   


                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.receiverEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "OTP verification Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);


                return CustomResult<bool>.Success(true);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }


        public async Task<CustomResult<bool>> SendPasswordResetEmailAsync(SendPasswordResetEmailVM model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/PasswordResetNotification.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{model.Fullname}");
                fileContents = fileContents.Replace("##EMAIL##", $"{model.receiverEmail}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##URL##", model.InstitutionBaseUrl);
                fileContents = fileContents.Replace("##PASSWORDRESETTOKEN##", model.PasswordResetToken);
                fileContents = fileContents.Replace("##EXPIRESON##", model.ExpiresOn.ToString());
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.receiverEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Password Reset Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);
                _logger.LogInformation($"Email Notification on Password reset: {JsonConvert.SerializeObject(logmodel)}");

                return CustomResult<bool>.Success(true);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when sending Password Reset email");
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<bool>> SendNewDefaultPasswordEmailAsync(SendWelcomeEmailVM model)
        {
            try
            {
                //string projectRootPath = _hostingEnvironment.ContentRootPath;
                //string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/NewDefaultPasswordEmail.html");

                //string fileContents = File.ReadAllText(confirmationEmailPath);
                string fileContents = EmailTemplates.NewPasswordEmailTemplate();

                fileContents = fileContents.Replace("##NAME##", $"{model.Fullname}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##EMAIL##", model.receiverEmail);
                fileContents = fileContents.Replace("##PASSWORD##", model.Password);
                fileContents = fileContents.Replace("##URL##", model.InstitutionBaseUrl);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.receiverEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "New Default Credentials";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);


                return CustomResult<bool>.Success(true);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
        public async Task<CustomResult<bool>> SendWelcomeEmailAsync(SendWelcomeEmailVM model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/WelcomeToqguardbackendEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{model.Fullname}");
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##EMAIL##", model.receiverEmail);
                fileContents = fileContents.Replace("##PASSWORD##", model.Password);
                fileContents = fileContents.Replace("##URL##", model.InstitutionBaseUrl);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = model.receiverEmail;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Account Creation Email";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);


                return CustomResult<bool>.Success(true);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<bool>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<bool>> SendSupportMail(SendSupportEmailVM model)
        {
            try
            {
                var user = await _dbcontext.Users.FirstOrDefaultAsync
                    (u => u.Id == model.UserId.ToString());
                if (user is null)
                {
                    //resultModel.AddError("User does not exist");
                }

                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = _mailsettings.SupportEmailAddress;
                logmodel.Sender = user.Email;
                logmodel.Subject = $"Support Email from {user.FirstName} {user.LastName}";
                logmodel.MessageBody = model.EmailContent;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                var cc = new[] { $"{user.Email}" };
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody, "", "", cc);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);

            }
            catch (Exception)
            {

            }

            return CustomResult<bool>.Success(true);
        }

        public async Task<CustomResult<bool>> SendContactUsEmail(SendContactUsEmailVM model)
        {
            try
            {
                model.EmailContent += "\n";
                if (_mailsettings.LogContactUsEmail)
                {
                    EmailLog logmodel = new();
                    logmodel.Receiver = _mailsettings.ContactUsEmailAddress;
                    logmodel.Sender = model.ProspectEmailAddress;
                    logmodel.Subject = $"Contact Us Mail from {model.Name} with email address {model.ProspectEmailAddress}";
                    logmodel.MessageBody = model.EmailContent;
                    logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                    logmodel.IsSent = false;
                    return CustomResult<bool>.Success(true);
                }
                else if (_mailsettings.SendContactUsEmail)
                {
                    bool result = SendMail($"Contact Us Mail from {model.ProspectEmailAddress}", _mailsettings.ContactUsEmailAddress, model.EmailContent);
                }
                else if (_mailsettings.LogAndSendContactUsEmail)
                {
                    EmailLog logmodel = new();
                    logmodel.Receiver = _mailsettings.ContactUsEmailAddress;
                    logmodel.Sender = model.ProspectEmailAddress;
                    logmodel.Subject = $"Contact Us Mail from {model.ProspectEmailAddress}";
                    logmodel.MessageBody = model.EmailContent;
                    logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                    logmodel.IsSent = false;
                    bool result = SendMail($"Contact Us Mail from {model.ProspectEmailAddress}", _mailsettings.ContactUsEmailAddress, model.EmailContent);

                    if (result)
                    {
                        logmodel.DateSent = DateTime.Now;
                        logmodel.IsSent = true;
                        logmodel.Retires++;
                    }
                    else
                    {
                        logmodel.IsSent = false;
                        logmodel.Retires++;
                    }
                    await LogEmail(logmodel);
                }

            }
            catch (Exception)
            {

            }
            return CustomResult<bool>.Success(true);
        }

        public async Task<bool> EmailConfirmation(string token, string email, string fullName, string userId, string url)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;

                var callbackUrl = url + $"user/activate?userId={userId}&token={token}";
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ConfirmationEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{fullName}");
                fileContents = fileContents.Replace("##ACTIVATIONLINK##", callbackUrl);
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##LOGINLINK##", url);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = email;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Email Verification Notification";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);
                return true;
            }
            catch (Exception)
            {
                //Log.Error(ex);
                return false;
            }
        }

        public async Task<string> EmailConfirmations(string token, string email, string fullName, string userId, string url)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;

                var callbackUrl = url + $"user/activate?userId={userId}&token={token}";
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ConfirmationEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{fullName}");
                fileContents = fileContents.Replace("##ACTIVATIONLINK##", callbackUrl);
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = email;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Email Verification Notification";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);
                return "true";
            }
            catch (Exception ex)
            {
                //Log.Error(ex);
                return $"Message is {ex.Message} InnerException is {ex.InnerException} stacktrace is{ex.StackTrace}";
            }
        }

        public bool SendMail(string subject, string email, string content, string fromName = null, string fromEmail = null, string[] cc = null)
        {
            SmtpClient smtpClient = new SmtpClient();
            MailMessage message = new MailMessage();
            if (cc != null)
            {
                if (cc.Any())
                {
                    foreach (var item in cc)
                    {
                        message.CC.Add(new MailAddress(item));
                    }
                }
            }
            string senderEmail = !string.IsNullOrEmpty("") ? "shcoolEmail" : _mailsettings.MailFrom;

            string MailFromName = fromName != null ? fromName : _mailsettings.MailFromName;
            try
            {
                MailAddress fromAddress;
                if (!string.IsNullOrEmpty(fromEmail))
                {
                    fromAddress = new MailAddress(fromEmail);
                }
                else
                {
                    fromAddress = new MailAddress(senderEmail, MailFromName);
                }
                message.From = fromAddress;
                message.To.Add(new MailAddress(email, email));

                message.Subject = subject;
                message.Body = content;
                message.IsBodyHtml = true;

                smtpClient.Host = _mailsettings.SMTPServer;
                int portno = 25;
                int.TryParse(_mailsettings.SMTPPORT, out portno);
                smtpClient.Port = portno;
                smtpClient.Credentials = new System.Net.NetworkCredential(_mailsettings.SMTPUserName, _mailsettings.SMTPPassword); //not neccessary
                smtpClient.EnableSsl = true;
                smtpClient.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred in while sending email to: {message.To} with subject {message.Subject} ");
                //Log.Error(ex);
                return false;
            }
            finally
            {
                smtpClient.Dispose();
                message.Dispose();
            }
        }

        public async Task<bool> SendMails(string subject, string email, string content, string fromEmail = null, string[] cc = null)
        {
            EmailLog logmodel = new EmailLog();
            logmodel.Receiver = "hh";
            logmodel.Sender = "";
            logmodel.Subject = "";
            logmodel.DateCreated = DateTimeOffset.Now;
            logmodel.DateToSend = DateTimeOffset.Now;
            logmodel.Retires = 1;
            logmodel.IsSent = true;
            SmtpClient smtpClient = new SmtpClient();
            MailMessage message = new MailMessage();
            if (cc != null)
            {
                if (cc.Any())
                {
                    foreach (var item in cc)
                    {
                        message.CC.Add(new MailAddress(item));
                    }
                }
            }
            try
            {
                MailAddress fromAddress;
                if (!string.IsNullOrEmpty(fromEmail))
                {
                    fromAddress = new MailAddress(fromEmail);
                }
                else
                {
                    fromAddress = new MailAddress(_mailsettings.MailFrom, _mailsettings.MailFromName);
                }
                message.From = fromAddress;
                message.To.Add(new MailAddress(email, email));

                message.Subject = subject;
                message.Body = content;
                message.IsBodyHtml = true;

                smtpClient.Host = _mailsettings.SMTPServer;
                int portno = 25;
                int.TryParse(_mailsettings.SMTPPORT, out portno);
                smtpClient.Port = portno;
                smtpClient.Credentials = new System.Net.NetworkCredential(_mailsettings.SMTPUserName, _mailsettings.SMTPPassword); //not neccessary
                smtpClient.EnableSsl = true;
                logmodel.MessageBody = $"_mailsettings.SMTPServer is {_mailsettings.SMTPServer}, smtpClient.Host is {smtpClient.Host}, _mailsettings.SMTPPassword is {_mailsettings.SMTPPassword}, _mailsettings.SMTPUserName is {_mailsettings.SMTPUserName}, smtpClient.Port is {smtpClient.Port}, portno is {portno}";
                await LogEmail(logmodel);
                smtpClient.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                logmodel.MessageBody = $"ex.Message is {ex.Message}, ex.StackTrace is {ex.StackTrace}, ex.InnerException is {ex.InnerException}";
                await LogEmail(logmodel);
                //Log.Error(ex);
                return false;
            }
            finally
            {
                smtpClient.Dispose();
                message.Dispose();
            }
        }

        public async Task<bool> SendContactFormEmail(string email, ContactModel model)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ContactTemplate.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##CONTACT_MESSAGE##", model.Message);
                fileContents = fileContents.Replace("##CONTACT_PHONE##", model.PhoneNumber);
                fileContents = fileContents.Replace("##CONTACT_COMPANY##", model.Company ?? string.Empty);
                fileContents = fileContents.Replace("##CONTACT_NAME##", model.Name);
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = _mailsettings.MailFrom;
                logmodel.Sender = model.Email;
                logmodel.Subject = "Enquiry";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);
                return true;
            }
            catch (Exception ex)
            {
                //Log.Error(ex);
                return false;
            }
        }

        public async Task<bool> Notify(string email, string subject, string fullName, string message, string title)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;

                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/Notification.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##TITLE##", $"{title}");
                fileContents = fileContents.Replace("##NAME##", $"{fullName}");
                fileContents = fileContents.Replace("##MESSAGE##", message);
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = email;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = subject;
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UnverifiedAccountPasswordLink(string token, string email, string fullName, string userId, string url)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;

                //var callbackUrl = $"{_settings.EmailURL}/#/Confirmation/{userId}?token={token}";
                //var callbackUrl = $"{_settings.EmailURL}user/activate?userId={userId}&token={token}";
                var callbackUrl = url + $"user/setpassword?userId={userId}&token={token}";
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ConfirmationEmail.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{fullName}");
                fileContents = fileContents.Replace("##ACTIVATIONLINK##", callbackUrl);
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = email;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Set Account  Password Notification";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);
                return true;
            }
            catch (Exception ex)
            {
                //Log.Error(ex);
                return false;
            }
        }

        public async Task<EmailLog> LogEmail(EmailLog model)
        {
            model = (await _dbcontext.EmailLogs.AddAsync(model)).Entity;
            int changes = await _dbcontext.SaveChangesAsync();
            return model;
        }

        public async Task<List<EmailLog>> GetUnsentMail(int count)
        {
            var result = await _dbcontext.EmailLogs.Where(x => !x.IsSent).Take(count).ToListAsync();

            return result;
        }

        public async Task<bool> UpdateMailAfterSent(EmailLog model)
        {
            var entity = await _dbcontext.Set<EmailLog>().FirstOrDefaultAsync(x => x.Id == model.Id);

            if (entity != null)
            {
                entity.DateSent = model.DateSent;
                entity.IsSent = model.IsSent;
                entity.Retires = model.Retires;
            }
            int count = await _dbcontext.SaveChangesAsync();
            return count > 0;
        }

        public async Task<bool> ForgetPasswordEmail(string token, string email, string fullName, string userId, string url, string schoolLogo)
        {
            try
            {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                var callbackUrl = "";
                callbackUrl = url + $"user/reset-password?userId={userId}&token={token}";
                string confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/PasswordResetNotification.html");

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{fullName}");
                fileContents = fileContents.Replace("##ACTIVATIONLINK##", callbackUrl);
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##LOGO##", schoolLogo);

                EmailLog logmodel = new EmailLog();
                logmodel.Receiver = email;
                logmodel.Sender = _mailsettings.MailFrom;
                logmodel.Subject = "Password Reset Notification";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, fileContents);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                await LogEmail(logmodel);
                return true;
            }
            catch (Exception ex)
            {
                //Log.Error(ex);
                return false;
            }
        }

        public EmailLog EmailConfirmationV2(string token, string email, string fullName, string userId, string url, string userType, string schoolLogo, string schoolName = null, string password = null)
        {
            var logmodel = new EmailLog();
            try
            {
                if (!schoolLogo.Contains(_azureSetting.CDN))
                {
                    schoolLogo = $"{_azureSetting.CDN}/{schoolLogo}";
                }
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                string confirmationEmailPath = string.Empty;
                string templateName = string.Empty;
                var callbackUrl = url + $"user/activate?userId={userId}&token={token}";
                if (!string.IsNullOrEmpty(userType))
                {
                    if (userType.Equals("student", StringComparison.OrdinalIgnoreCase))
                    {
                        templateName = "EmailTemplate/StudentConfirmationEmail.html";
                        confirmationEmailPath = Path.Combine(projectRootPath, templateName);
                    }
                    else if (userType.Equals("lecturer", StringComparison.OrdinalIgnoreCase))
                    {
                        templateName = "EmailTemplate/LecturerConfirmationEmail.html";
                        confirmationEmailPath = Path.Combine(projectRootPath, templateName);
                    }
                    else if (userType.Equals("staffuser", StringComparison.OrdinalIgnoreCase))
                    {
                        templateName = "EmailTemplate/StaffUserConfirmationEmail.html";
                        confirmationEmailPath = Path.Combine(projectRootPath, templateName);
                    }
                    else if (userType.Equals("migration", StringComparison.OrdinalIgnoreCase))
                    {
                        templateName = "EmailTemplate/UserMigrationEmail.html";
                        confirmationEmailPath = Path.Combine(projectRootPath, templateName);
                    }
                    else if (userType.Equals("migration_error", StringComparison.OrdinalIgnoreCase))
                    {
                        templateName = "EmailTemplate/UserMigrationErrorEmail.html";
                        confirmationEmailPath = Path.Combine(projectRootPath, templateName);
                    }
                    else
                    {
                        confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ConfirmationEmail.html");
                    }
                }
                else
                {
                    confirmationEmailPath = Path.Combine(projectRootPath, "EmailTemplate/ConfirmationEmail.html");
                }

                string fileContents = File.ReadAllText(confirmationEmailPath);
                fileContents = fileContents.Replace("##NAME##", $"{fullName}");
                fileContents = fileContents.Replace("##ACTIVATIONLINK##", callbackUrl);
                fileContents = fileContents.Replace("##SENDEREMAIL##", _mailsettings.MailFrom);
                fileContents = fileContents.Replace("##LOGO##", schoolLogo);
                fileContents = fileContents.Replace("##SCHOOLNAME##", schoolName);
                fileContents = fileContents.Replace("##LOGINLINK##", url);

                if (!string.IsNullOrEmpty(password))
                {
                    fileContents = fileContents.Replace("##PASSWORD##", password);
                }

                logmodel.Receiver = email;
                logmodel.Sender = !string.IsNullOrEmpty("") ? "" : _mailsettings.MailFrom;
                logmodel.Subject = "Email Verification Notification";
                logmodel.MessageBody = fileContents;
                logmodel.DateCreated = logmodel.DateToSend = DateTime.Now;
                logmodel.IsSent = false;
                logmodel.CC = "None";
                logmodel.BCC = "None";
                logmodel.CorrellationId = "None";
                logmodel.Template_Code = templateName;
                bool result = SendMail(logmodel.Subject, logmodel.Receiver, logmodel.MessageBody, schoolName);
                if (result)
                {
                    logmodel.DateSent = DateTime.Now;
                    logmodel.IsSent = true;
                    logmodel.Retires++;
                }
                else
                {
                    logmodel.IsSent = false;
                    logmodel.Retires++;
                }
                return logmodel;
            }
            catch (Exception)
            {
                logmodel.IsSent = false;
                return logmodel;
            }
        }
    }
}