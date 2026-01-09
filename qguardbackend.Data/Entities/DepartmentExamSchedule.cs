using AutoMapper;
using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class DepartmentExamSchedule : BaseEntity
    {
        public long? DepartmentId { get; set; }
        public Department Department { get; set; }
        public long? ExamScheduleId { get; set; }
        public ExamSchedule ExamSchedule { get; set; }
    }
}
