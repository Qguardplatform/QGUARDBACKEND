using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class ProctorConfiguration : BaseEntity
    {
        public static ProctorConfiguration Create(String key, String value, long examScheduleId)
        {
            return new ProctorConfiguration()
            {
                ParameterName = key,
                Value = value,
                ExamScheduleId = examScheduleId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public string ParameterName { get; set; }
        public string Value { get; set; }
        public long ExamScheduleId { get; set; }
        public ExamSchedule ExamSchedule { get; set; }
    }
}