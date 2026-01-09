using edutech.services.examportal.Core.Autofac;
using examportal.Data.Entities;
using edutech.services.examportal.Data.DTOs.ResponseDto;
using Microsoft.AspNetCore.Http;

namespace LS1.Service.ICServices.Interfaces;

public interface ITokenService //: IAutoDependencyCore
{
    Task<string> GetTokenAsync();
    Task<AuthClaims> GetUserClaim();
    void SetUserClaims(HttpContext context, string userId, string role);
    Task<string> generateResetTokenAsync();
    Task<string> generateVerificationTokenAsync();
    Task<string> GenerateRefreshToken(string email, string SAPId);
    Task<LoginResponse> GenerateJwtToken(string email, string applicationCode, ICollection<ApplicationRole> userRoles);
}