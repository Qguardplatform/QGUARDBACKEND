using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class AuditLog : BaseEntity
    {
        public static AuditLog Create(int eventType, String userName, string IpAddress, String action, 
            String description, long institutionId)
        {
            return new AuditLog()
            {
                UserId = userName,
                EventType = eventType,
                IPAddress = IpAddress,
                Action = action,
                Description = description,
                InstitutionId = institutionId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string UserId { get; set; }
        public int EventType { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
        public long? InstitutionId { get; set; }
        //public Institution Institution { get; set; }
    }
}
