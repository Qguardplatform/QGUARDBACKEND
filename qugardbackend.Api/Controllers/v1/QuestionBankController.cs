using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using examportal.Api.Controllers;
using examportal.Filter;
using Microsoft.AspNetCore.Mvc;

namespace examportal.Controllers.v1
{
    [ApiVersion("1.0")]
    public class QuestionBankController : BaseController
    {
        private readonly IQuestionBankService _questionBankService;

        public QuestionBankController(IQuestionBankService questionBankService)
        {
            _questionBankService = questionBankService;
        }

        /// <summary>
        /// Endpoint to create questions to save in the question bank
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateQuestion([FromForm] QuestionBankCreateModel model)
        {
            var response = await _questionBankService.CreateQuestionAsync(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Get all the questions in the question bank, filter down to the question bank Tag 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("all-questions")]
        public async Task<IActionResult> GetAllQuestions([FromQuery] QuestionBankQueryModelMini query)
        {
            var response = await _questionBankService.GetAllQuestionsAsync(query);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// get all questions Banks grouped by Tag and course, counting the questions on each
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        [HttpGet("all-question-banks-in-group")]
        [ProducesResponseType(StatusCodes.Status200OK, Type =
            typeof(Response<CustomResult<PaginatedResult<ExamQuestionBankListDto>>>))]
        public async Task<IActionResult> GetAllQuestionBanksAsync([FromQuery] QuestionBankQueryModelMini search)
        {
            var response = await _questionBankService.GetAllQuestionBanksAsync(search);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Get a single question by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById(long id)
        {
            var response = await _questionBankService.GetQuestionByAsyncId(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// update a question in the question bank
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("update-question-bank-question")]
        public async Task<IActionResult> UpdateQuestionBank(QuestionBankUpdateModel model)
        {
            var response = await _questionBankService.UpdateQuestionAsync(model.QuestionBankId, model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Update the option content of a question option content
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>

        [HttpPost("update-question-bank-question-option")]
        public async Task<IActionResult> UpdateQuestionOptionAsync(QuestionOptionUpdateModel model)
        {
            var response = await _questionBankService.UpdateQuestionOptionAsync(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Add more option to a question bank
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("add-question-bank-question-option")]
        public async Task<IActionResult> AddQuestionOptionAsync(AddQuestionOptionModel model)
        {
            var response = await _questionBankService.AddQuestionOptionAsync(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }



        /// <summary>
        /// update an exam schedule question
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("update-exam-schedule-question")]
        public async Task<IActionResult> UpdateQuestion(UpdateExamScheduleQuestionBankModel request)
        {
            var response = await _questionBankService.UpdateExamBankQuestionAsync(request.examscheduleId, request.QuestionToEdit);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        /// <summary>
        /// Enable and disable a question in a question bank
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPatch("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleQuestionStatus(long id)
        {
            var response = await _questionBankService.ToggleQuestionStatusAsync(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Soft delete a question this disables a question
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteQuestion(long id)
        {
            var response = await _questionBankService.DeleteQuestionAsync(id);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("delete-multiple")]
        public async Task<IActionResult> DeleteMultipleQuestions([FromBody] DeleteMultipleQuestionsRequest model)
        {
            if (model == null || model.QuestionIds == null || !model.QuestionIds.Any())
            {
                return BadRequest(new
                {
                    code = ResponseCodes.BadRequestErrorCode,
                    message = "Please provide at least one question ID."
                });
            }
            var response = await _questionBankService.DeleteMultipleQuestionsAsync(model.QuestionIds);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("question-download-sample")]
        public IActionResult DownloadSampleCsv()
        {
            var fileBytes = _questionBankService.GenerateSampleCsv(out string fileName, out string contentType);
            return File(fileBytes, contentType, fileName);
        }

        [HttpPost("questions-bulk-upload")]
        public async Task<IActionResult> BulkQuestionUpload([ModelBinder(typeof(JsonWithFilesFormDataModelBinder))] QuestionBankUploadModel model)
        {
            var response = await _questionBankService.UploadQuestionAsync(model);
            if (response.ResponseCode != ResponseCodes.SuccessCode)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}