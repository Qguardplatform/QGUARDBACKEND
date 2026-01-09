using qguardbackend.Data.Abstracts;
using qguardbackend.Data.Enums;

namespace qguardbackend.Data.Entities
{
    public class QuestionBank : BaseEntity
    {
        public static QuestionBank Create(String question, DifficultLevel level, bool isMultipleChoice,
            String tags, long courseId, long institutionId, int point)
        {
            return new QuestionBank()
            {
                Question = question,
                DifficultLevel = level.ToString(),
                Tags = tags,
                CourseId = courseId,
                Points = point,
                IsMultipleChoice = isMultipleChoice,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                InstitutionId = institutionId
            };
        }
        public string Question { get; set; }
        public string DifficultLevel { get; set; } // enums => hard, medium, easy
        public string Tags { get; set; } // getting started, exam for students in special class, etc
        public int Points { get; set; } // Scoring for this question in this exam
        public string ImageUrl { get; set; }
        public bool IsMultipleChoice { get; set; } // If multiple correct options are allowed

        public long? CourseId { get; set; }
        public Course Course { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }

        public ICollection<QuestionOption> QuestionOptions { get; set; } = new HashSet<QuestionOption>();
    }
}
