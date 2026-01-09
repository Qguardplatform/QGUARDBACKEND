using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using examportal.Filter.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using System.Text;

namespace examportal.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BaseController : ControllerBase
{
    public UserPrincipal CurrentUser
    {
        get
        {
            return new UserPrincipal(User as ClaimsPrincipal);
        }
    }
    public static string GetModelStateErrors(ModelStateDictionary modelState)
    {
        StringBuilder result = new StringBuilder();
        var err = modelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage);
        foreach (var item in err)
        {
            result.Append(item + Environment.NewLine);
        }
        return result.ToString().Replace(Environment.NewLine, " ");
    }

    internal CustomResult<bool> GetValidationErrors<T>(ModelStateDictionary modelState)
    {
        CustomError error = new(ValidationErrors(modelState));
        return CustomResult<bool>.Failure(error, ResponseCodes.ModelValidationErrorCode);
    }

    private List<string> ValidationErrors(ModelStateDictionary modelState)
    {
        return modelState?
            .Where(state => state.Value?.Errors != null)
            .SelectMany(state => state.Value?.Errors!)
            .Where(error => !string.IsNullOrWhiteSpace(error.ErrorMessage))
            .Select(error => error.ErrorMessage)
            .ToList() ?? new List<string>() { "Validator errors" };
    }
}