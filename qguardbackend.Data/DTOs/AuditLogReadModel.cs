namespace qguardbackend.Data.DTOs
{
    public class AuditLogReadModel
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string EventType { get; set; }
        public string IPAddress { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public InstitutionResponseDto Institution { get; set; }
    }

    public class AuditLogWriteModel
    {
        public string UserId { get; set; }
        public int EventType { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
        public long InstitutionId { get; set; }
        public DateTime CreatedAt { get; set; } 
    }

    public class AuditLogFilterModel : QueryModelMini
    {
        public string? Action { get; set; }
        public string? EventType { get; set; }
        public string? CreatedBy { get; set; }
        //public long? InstitutionId { get; set; }
    }
}