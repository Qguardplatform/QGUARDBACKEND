namespace qguardbackend.Data.DTOs.ResponseDto
{   
    public  class ExamQuestionBankListDto
    {
        public string Tags { get; set; }
        public long CourseId { get; set; }
        public string CourseName { get; set; }
        public List<long> QuestionIds { get; set; }
        public List<string> DifficultLevel { get; set; }
        public List<string> QuestionType { get; set; } //subjective, Elective
        public long NoOfQuestions { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class DeleteMultipleQuestionsRequest
    {
        public List<long> QuestionIds { get; set; }
    }
}