using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class QuestionOption : BaseEntity
    {
        public string OptionTag { get; set; } //A,B,C,D
        public string OptionDescription { get; set; }
        public bool IsCorrectOption { get; set; }
        public string ImageUrl { get; set; }
        public long? QuestionBankId { get; set; }
        public QuestionBank QuestionBank { get; set; }
    }
}
