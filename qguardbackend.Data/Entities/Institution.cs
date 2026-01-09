using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Institution : BaseEntity
    {
        public static Institution Create(String name, String code, string type, String adminName, String adminEmail, string hostName,
            String SSORequired, String senderEmail, String language, String primaryColor, String secondaryColor)
        {
            return new Institution()
            {
                Name = name,
                Code = code,
                InstitutionType = type,
                AdminEmail = adminEmail,
                AdminName = adminName,
                HostName  = hostName,
                SSORequired = SSORequired,
                SenderEmail = senderEmail,
                DefaultLanguage = language,
                PrimaryThemeColor = primaryColor,
                SecondaryThemeColor = secondaryColor,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Logo { get; set; }
        public string AdminName { get; set; }
        public string AdminEmail { get; set; }
        public string HostName { get; set; }
        public string SSORequired { get; set; }
        public string SsoURL { get; set; }
        public string SsoLogo { get; set; }
        public string InstitutionType { get; set; }
        public string SenderEmail { get; set; }
        public string DefaultLanguage { get; set; }
        public string PrimaryThemeColor { get; set; }
        public string SecondaryThemeColor { get; set; }
    }
}
