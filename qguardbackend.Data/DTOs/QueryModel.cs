using qguardbackend.Data.Enums;

namespace qguardbackend.Data.DTOs
{
    public class QueryModelMini : BaseQueryModel
    {
        public bool? IsActive { get; set; }
        public string SearchWord { get; set; }
        public string Sorting { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class GetProgramQueryModelMini : BaseQueryModel
    {
        public ProgramTypeEnum? ProgramType { get; set; }
        public bool? IsActive { get; set; }
        public string SearchWord { get; set; }
        public string Sorting { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class GetDeptQueryModelMini : BaseQueryModel
    {
        public long? FacultyId { get; set; }
        public bool? IsActive { get; set; }
        public string SearchWord { get; set; }
        public string Sorting { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class BaseQueryModel
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }

    public class FacultyQueryModelMini : QueryModelMini
    {
        public long? ProgramId { get; set; }
    }

    public class QuestionBankQueryModelMini : QueryModelMini
    {
        public string Tag { get; set; }
        public string DifficultLevel { get; set; }
        public bool? ExamType { get; set; }
        public long CourseId { get; set; }
    }

    public class CourseQueryModelMini : QueryModelMini
    {
        public long? DepartmentId { get; set; }
        public long? TutorId { get; set; }
        public long? Level { get; set; }
    }
}