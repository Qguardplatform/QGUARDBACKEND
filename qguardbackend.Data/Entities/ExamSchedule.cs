using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class ExamSchedule : BaseEntity
    {
        public string Title { get; set; } // E.g. "GST 201"
        public string Description { get; set; } // Full course name or instructions

        public long? AcademicSessionId { get; set; } // e.g. "2024/2025"
        public Session AcademicSession { get; set; }

        public long? SemesterId { get; set; } // e.g. "Semester 1"
        public Semester Semester { get; set; }

        public long CourseId { get; set; }
        public Course Course { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }

        public int? MaxQuestions { get; set; } // Total number of questions (used in random selection if needed)
        public int? PassScore { get; set; }
        public int? ExamDurationInMinutes { get; set; }

        public bool IsRandomized { get; set; } // "Make questions appear random" checkbox
        public bool EnableAiProctoring { get; set; }
    

        public bool InstantAnswerGrading { get; set; }
        public bool InstantResultPublishing { get; set; }
        public string Instruction { get; set; }

        public bool MakeQuestionsAppearRandom { get; set; } // If not randomized, this defines question order

        public long? LevelId { get; set; }
        public Level Level { get; set; } // E.g. "100", "200", etc

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool AllowLateStart { get; set; }

        public string Status { get; set; } // Published, Draft, etc.
        public bool IsPublished { get; set; } = false;

        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; }
        public virtual ICollection<DepartmentExamSchedule> DepartmentExamSchedule { get; set; }
        public virtual ICollection<ProctorConfiguration> ProctorConfigurations { get; set; }

        // flags to avoid duplicate mails
        public bool PublishedMailSent { get; set; }
        public bool ReminderMailSent { get; set; }
        public bool StartingSoonMailSent { get; set; }
        //public long? AssignProctor { get; set; }
        public long? AssignProctorId { get; set; }
    
    }
}