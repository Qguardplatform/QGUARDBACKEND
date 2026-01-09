using edutech.services.examportal.Data.Abstracts;

namespace edutech.services.examportal.Data.Entities
{
    public class Question : BaseEntity
    {
        public static Question Create(long examQuestionId, long questionBankId)
        {
            return new Question()
            {
                ExamQuestionId = examQuestionId,
                QuestionBankId = questionBankId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }
        public long ExamQuestionId { get; set; }
        public ExamQuestion ExamQuestion { get; set; }

        public long QuestionBankId { get; set; }
        public QuestionBank QuestionBank { get; set; }
    }
}
