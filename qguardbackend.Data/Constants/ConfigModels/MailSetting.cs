namespace SISService.Core.ConfigModels
{
    public class MailSetting
    {
        public string MailFrom { get; set; }
        public string MailFromName { get; set; }
        public string SMTPServer { get; set; }
        public string SMTPUserName { get; set; }
        public string SMTPPassword { get; set; }
        public string SMTPPORT { get; set; }
        public string SupportEmailAddress { get; set; }
        public string ContactUsEmailAddress { get; set; }
        public bool LogContactUsEmail { get; set; }
        public bool SendContactUsEmail { get; set; }
        public bool LogAndSendContactUsEmail { get; set; }
      
    }
}
