using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.Results;
using examportal.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class ExamQuestionController : BaseController
    {
        private readonly IExamService _examService;

        public ExamQuestionController(IExamService examService)
        {
            _examService = examService;
        }
        /// <summary>
        /// Submit answers for an exam schedule
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("submit-answers")]
        public async Task<IActionResult> SubmitQuestionAnswers([FromBody]ExamSubmissionRequestDto model)
        {
            var response = await _examService.SubmitExaminationAnswersAsync(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// End point to add new questions under an exam schedule
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("create-multi-exam-question")]
        public async Task<IActionResult> CreateExam([FromBody] ExamQuestionCreateModel model)
        {
            var response = await _examService.CreateQuestionForExamAsync(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Map existing multiple Questions to Exam Schedule
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("map-multiple-existing-questions-to-an-exam-schedule")]
        public async Task<IActionResult> MapQuestionsToExamSchedule([FromBody] MapQuestionsToExamScheduleRequestDto model)
        {
            var response = await _examService.MapQuestionsToExamSchedule(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Endpoint to add new single question under an exam schedule
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("create-single-exam-question")]
        public async Task<IActionResult> CreateSingleExamQuestion([FromBody] SingleExamQuestionCreateModel model)
        {
            var response = await _examService.CreateSingleQuestionForExamAsync(model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// un-map single exam question from an exam schedule
        /// </summary>
        /// <param name="examscheduleId"></param>
        /// <param name="questionbankId"></param>
        /// <returns></returns>
        [HttpPost("unmap-single-exam-question-from-exam-schedule")]
        public async Task<IActionResult> CreateSingleExamQuestion(long examscheduleId, long questionbankId)
        {
            var response = await _examService.RemoveMappedSingleQuestionFromExamAsync(examscheduleId, questionbankId, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
     

        /// <summary>
        /// Get the Question summary of all exam schedules
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<ExamQuestionListModel>>>))]
        public async Task<IActionResult> GetAll([FromQuery] ExamQuestionFilterModel search)
        {
            var response = await _examService.GetAllAsync(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //GetQuestionsAndOptionsNoCorrectOption
        /// <summary>
        /// get all questions in exam schedule with options but not showing correct option for each question
        /// </summary>
        /// <param name="examScheduleId"></param>
        /// <param name="search"></param>
        /// <returns></returns>        
        [HttpGet("exam-schedule-questions-and-options-no-answer/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<ExamQuestionListDto>>>))]
        public async Task<IActionResult> GetAllQuestionsInExamScheduleAsync([FromRoute]long examScheduleId, [FromQuery] QueryModelMini search)
        {
            var response = await _examService.GetQuestionsAndOptionsNoCorrectOption(examScheduleId, search.PageNumber, search.PageSize);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// get all questions in exam schedule with options but showing the correct option for each question
        /// </summary>
        /// <param name="examScheduleId"></param>
        /// <param name="PageNumber"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        [HttpGet("exam-schedule-questions-and-options-with-answers/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ExamCreateModel>>))]
        public async Task<IActionResult> QuestionsInExamSchedule([FromRoute] long examScheduleId, [FromQuery] int? PageNumber, [FromQuery] int? PageSize)
        {
            var response = await _examService.QuestionsInExamSchedule(examScheduleId, PageNumber, PageSize);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// get all questions in exam schedule with options but showing the correct option for each question for a candidate alone with the selected option
        /// </summary>
        /// <param name="examScheduleId"></param>
        /// <param name="candidateuserid"></param>
        /// <param name="PageNumber"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        [HttpGet("exam-schedule-questions-and-options-with-answers-for-a-candidate/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ExamCreateModel>>))]
        [AllowAnonymous]
        public async Task<IActionResult> QuestionsInExamSchedule([FromRoute] long examScheduleId, [FromQuery] string candidateuserid, [FromQuery] int? PageNumber, [FromQuery] int? PageSize)
        {
            var response = await _examService.QuestionsInExamScheduleForACandidate(candidateuserid, examScheduleId, PageNumber, PageSize);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        
        /// <summary>
        /// Get the Question summary of a single exam schedule
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById(long id)
        {
            var response = await _examService.GetByIdAsync(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Update the Questions attached to an exam schedule
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateQuestion(long id, [FromBody] ExamQuestionCreateDto model)
        {
            var response = await _examService.UpdateAsync(id, model, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Update candidate view count
        /// </summary>
        /// <param name="examScheduleId"></param>
        /// <param name="candidateId"></param>
        /// <returns></returns>
        [HttpPost("updatecandidateexamviewcount")]
        public async Task<IActionResult> UpdateCandidateViewCount(long examScheduleId, long candidateId)
        {
            var response = await _examService.UpdateCandidateViewCount(examScheduleId, candidateId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Removing a question from an Exam schedule using the exam question id: from the examSchedule and Questions
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> RemoveQuestionFromSchedule(long id)
        {
            var response = await _examService.DeleteAsync(id, CurrentUser.Email);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Get the exam schedules for a candidate, this validates the candidates current, level, session, semester, session
        /// </summary>
        /// <param name="candidateUserId"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        [HttpGet("candidate-published-exam-schedule/{candidateUserId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<CandidateExamScheduleModel>>>))]
        [AllowAnonymous]
        public async Task<IActionResult> PublishedExamSchedule([FromRoute]string candidateUserId, [FromQuery] QueryModelMini search)
        {
            var response = await _examService.CandidatePulishedExamSchedule(candidateUserId, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Get the Instruction on an exam schedule
        /// </summary>
        /// <param name="examScheduleId"></param>
        /// <returns></returns>
        [HttpGet("exam-instruction/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ExamQuestionListDto>>))]
        public async Task<IActionResult> ExamInstruction([FromRoute] long examScheduleId)
        {
            var response = await _examService.GetExamInstruction(examScheduleId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("candidate-exam-list/{candidateId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<CandidateExamListModel>>))]
        public async Task<IActionResult> ExamList([FromRoute] string candidateId, [FromQuery] ExamQuestionFilterModel search)
        {
            var response = await _examService.CandidateResultList(candidateId, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("candidate-examination-details/{examScheduleId}/{candidateId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<ExamSubmissionDetailModel>>))]
        public async Task<IActionResult> ExaminationList([FromRoute] long examScheduleId, [FromRoute] long candidateId, [FromQuery] int? PageNumber, [FromQuery] int? PageSize)
        {
            var response = await _examService.GetCandidateExaminationDetails(examScheduleId, candidateId, PageNumber, PageSize);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Get the list of candidate exam results by candidate Id
        /// </summary>
        [HttpGet("candidate-exam-result-list/{candidateId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<PaginatedResult<StudentExamsDto>>>))]
        public async Task<IActionResult> CandidateExamsResultList([FromRoute] long candidateId, [FromQuery] StudentExamDoneFilterModel search)
        {
            var response = await _examService.CandidateExamsResultList(candidateId, search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Get the details of candidate exam results by candidate Id and exam schedule Id
        /// </summary>
        [HttpGet("candidate-exam-results-list-by-exam-schedule/{candidateId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<StudentExamDetailsDto>>))]
        public async Task<IActionResult> CandidateExamsResultDetailsByExamSchedule([FromRoute] long candidateId, [FromQuery] long examScheduleId)
        {
            var response = await _examService.CandidateExamsResultDetailsByExamSchedule(candidateId, examScheduleId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Export candidate exams
        /// </summary>
        [HttpGet("export-candidate-exam/{examScheduleId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Response<CustomResult<StudentExamDetailsDto>>))]
        public async Task<IActionResult> ExportExam([FromRoute] long examScheduleId)
        {
            var response = await _examService.ExportCandidateExams(examScheduleId);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}