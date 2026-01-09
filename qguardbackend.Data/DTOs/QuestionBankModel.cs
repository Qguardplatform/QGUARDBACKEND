using qguardbackend.Data.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class QuestionBankModel
    {
        public long Id { get; set; }
        public string Question { get; set; }
        public string DifficultLevel { get; set; }
        public string Type { get; set; }
        public string Tags { get; set; }
        public string IsMultipleChoice { get; set; }
        public string ImageUrl { get; set; }
        public int Point { get; set; }
        public long CourseId { get; set; }
        public CourseModel Course { get; set; }
        public InstitutionResponseDto Institution { get; set; }
        public DateTime DateCreated { get; set; }
        public List<QuestionOptionModel> Options { get; set; } = new();
    }


    public class QuestionBankMiniModel
    {
        public long Id { get; set; }
        public string Question { get; set; }
        public string DifficultLevel { get; set; }
        public string Type { get; set; }
        public string Tags { get; set; }

        public string ImageUrl { get; set; }
        public int Point { get; set; }
        public long CourseId { get; set; }
        public string CourseName { get; set; }
        public DateTime CreatedAt { get; set; }

    }

    public class QuestionOptionModel
    {
        public long Id { get; set; }
        public string OptionTag { get; set; }
        public string OptionDescription { get; set; }
        public bool IsCorrectOption { get; set; }
    }

    public class UpdateExamScheduleQuestionBankModel
    {

        [Required(ErrorMessage = "exam schedule Id is required!")]
        public long examscheduleId { get; set; }

        public QuestionBankUpdateModel QuestionToEdit { get; set; }
    }
    public class QuestionBankCreateModel
    {
        [Required(ErrorMessage = "Question is required!")]
        public string Question { get; set; }
        public DifficultLevel DifficultLevel { get; set; }
        [Required(ErrorMessage = "Question bank tag is required!")]
        public string Tags { get; set; }
        public bool IsMultipleChoice { get; set; }
        public IFormFile ImageUrl { get; set; } // Optional question image

        public long? CourseId { get; set; }
        //public long InstitutionId { get; set; }
        public int Point { get; set; }

        public List<OptionCreateModel> Options { get; set; }
    }


    public class QuestionBankUpdateModel
    {
        [Required(ErrorMessage = "Question bank Id is required!")]
        public long QuestionBankId { get; set; }
        [Required(ErrorMessage = "Question is required!")]
        public string Question { get; set; }
        public DifficultLevel DifficultLevel { get; set; }
        [Required(ErrorMessage = "Question bank tag is required!")]
        public string Tags { get; set; }
        public bool IsMultipleChoice { get; set; }
        //public IFormFile ImageUrl { get; set; } // Optional question image

        public long? CourseId { get; set; }
        //public long InstitutionId { get; set; }
        public int Point { get; set; }

        public List<OptionCreateModel> Options { get; set; }
    }



    public class QuestionOptionUpdateModel
    {
        [Required(ErrorMessage = "Question Option Id is required!")]
        public long QuestionOptionId { get; set; }
        [Required(ErrorMessage = "Question option content is required!")]
        public string UpdatedOptionContext { get; set; }

    }
    public class AddQuestionOptionModel
    {
        [Required(ErrorMessage = "Question Bank Id is required!")]
        public long QuestionBankId { get; set; }
        [Required(ErrorMessage = "Question option content is required!")]
        public string NewOptionContext { get; set; }
        [Required(ErrorMessage = "IsCorrectOption is required!")]
        public bool IsCorrectOption { get; set; }

    }

    public class ExamScheduleQuestionBankCreateModel
    {
        [Required(ErrorMessage = "Question is required!")]
        public string Question { get; set; }
        public DifficultLevel DifficultLevel { get; set; }
        [Required(ErrorMessage = "Question bank tag is required!")]
        public string Tags { get; set; }
        public bool IsMultipleChoice { get; set; }
        public IFormFile ImageUrl { get; set; } // Optional question image

        public long ExamScheduleId { get; set; }
        //public long InstitutionId { get; set; }
        public int Point { get; set; }

        public List<OptionCreateModel> Options { get; set; }
    }

    public class OptionCreateModel
    {
        //public string OptionTag { get; set; }
        public string OptionDescription { get; set; }
        public bool IsCorrectOption { get; set; }
        public IFormFile? Image { get; set; } // OR Image option
    }
    public class OptionForExamModel
    {
        public long Id { get; set; }
        public string OptionTag { get; set; }
        public string OptionDescription { get; set; }
        public bool IsCorrectOption { get; set; }
    }

    public class OptionForExamModelNoCorrectionOption
    {
        public long Id { get; set; }
        public string OptionTag { get; set; }
        public string OptionDescription { get; set; }
        //public bool IsCorrectOption { get; set; }
    }


    public class OptionForExamModelNoAnswer
    {
        public long Id { get; set; }
        public string OptionTag { get; set; }
        public string OptionDescription { get; set; }
        //public bool IsCorrectOption { get; set; }
    }

    public class QuestionBankUploadModel
    {
        public DifficultLevel DifficultLevel { get; set; }
        public string Tags { get; set; }
        public long CourseId { get; set; }
        public bool IsMultipleChoice { get; set; }
        public IFormFile File { get; set; }
    }
    public class MigrationErrorVM
    {
        public int RowNum { get; set; }
        public string ErrorMessage { get; set; }
        public string RecordIdentifier { get; set; }
        public string AdditionalMessage { get; set; }
    }

    public class QuestionUploadFormatModel
    {
        public string Question { get; set; }
        public string Options { get; set; }
        public string CorrectOption { get; set; }
        public string Points { get; set; }
    }
}
