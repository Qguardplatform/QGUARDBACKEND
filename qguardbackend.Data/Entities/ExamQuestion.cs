using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class ExamQuestion : BaseEntity
    {
        public static ExamQuestion Create(String instruction, long examScheduleId, bool randomQuestion, long questionBankId)
        {
            return new ExamQuestion()
            {
                Instruction = instruction,
                ExamScheduleId = examScheduleId,
                MakeQuestionsAppearRandom = randomQuestion,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                QuestionBankId = questionBankId
            };
        }
        public long ExamScheduleId { get; set; }
        public ExamSchedule ExamSchedule { get; set; }

        public string Instruction { get; set; }

        public bool MakeQuestionsAppearRandom { get; set; } // If not randomized, this defines question order
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
        public long? QuestionBankId { get; set; }
    
        public QuestionBank QuestionBank { get; set; }
    }
}
