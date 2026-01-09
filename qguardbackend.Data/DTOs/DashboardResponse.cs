using qguardbackend.Data.Enums;

namespace qguardbackend.Data.DTOs
{
    public class DashboardResponse
    {
        public UserBreakdownDto UserBreakdown { get; set; }
        public ExamStatusDto ExamStatus { get; set; }
        public ResultStatusDto ResultStatus { get; set; }
        public QuestionBankDto QuestionBank { get; set; }
    }

    public class UserBreakdownDto
    {
        public Dictionary<string, int> UserCounts { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsers { get; set; }
    }

    public class ExamStatusDto
    {
        public int Scheduled { get; set; }
        public int Ongoing { get; set; }
        public int Completed { get; set; }
        public List<RecentExamDto> RecentExaminations { get; set; }
    }

    public class ResultStatusDto
    {
        public int Published { get; set; }
        public int Unpublished { get; set; }
        public List<RecentResultDto> RecentlyPublished { get; set; }
    }

    public class QuestionBankDto
    {
        public int TotalQuestions { get; set; }
        public int NewQuestions { get; set; }
    }

    public class RecentExamDto
    {
        public string Title { get; set; }
        public int StudentCount { get; set; }
        public DateTime ScheduledDate { get; set; }
    }

    public class RecentResultDto
    {
        public string CourseTitle { get; set; }
        public string PublishedDate { get; set; }
    }

    public class DashboardFilterModel
    {
        public FilterRange Range { get; set; } = FilterRange.Last7Days;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ExamPerformanceDto
    {
        public double OverallAverageScore { get; set; }
        public double ImprovementRate { get; set; } 
        public List<CoursePerformanceDto> CoursePerformances { get; set; }
    }

    public class CoursePerformanceDto
    {
        public string CourseCode { get; set; }
        public double AverageScore { get; set; }
    }

    public class AdminDashboardResponse
    {
        public int TotalInstitutions { get; set; }

        public ActiveUserSummary ActiveUsers { get; set; }
        public ExamSummary Exams { get; set; }
        public List<UserCategoryDto> UserCategories { get; set; }
        public List<ExamStatisticDto> ExamStatistics { get; set; }
    }

    public class ActiveUserSummary
    {
        public int Count { get; set; }
        public int Total { get; set; }
    }

    public class ExamSummary
    {
        public int Taken { get; set; }
        public int Scheduled { get; set; }
    }

    public class UserCategoryDto
    {
        public string Institution { get; set; }
        public int TotalUsers { get; set; }
        public int Candidates { get; set; }
        public int Tutors { get; set; }
        public int Proctors { get; set; }
        public int ExamOfficers { get; set; }
        public int Admins { get; set; }
    }

    public class ExamStatisticDto
    {
        public string Institution { get; set; }
        public int Scheduled { get; set; }
        public int Ongoing { get; set; }
        public int Completed { get; set; }
    }
}