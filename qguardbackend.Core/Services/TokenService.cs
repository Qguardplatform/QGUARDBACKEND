using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using LS1.Service.ICServices.Interfaces;
using Microsoft.Extensions.Configuration;
using examportal.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using edutech.services.examportal.Data.DTOs.ResponseDto;
using edutech.services.examportal.Data.DbContext;

namespace edutech.services.examportal.Core.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<TokenService> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    //private readonly AppDbContext _context;

    public TokenService(IConfiguration config, IHttpContextAccessor httpContextAccessor,
        RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager,
        ILogger<TokenService> logger)
    {
        _config = config;
        _roleManager = roleManager;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string> generateResetTokenAsync()
    {
        // token is a cryptographically strong random sequence of values
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        var users = await _userManager.Users
            .Where(x => x.ActivationToken == token).ToListAsync();

        if (users != null && users.Count > 0)
            return await generateResetTokenAsync();

        return token;
    }

    public async Task<string> generateVerificationTokenAsync()
    {
        // token is a cryptographically strong random sequence of values
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        var users = await _userManager.Users
            .Where(x => x.ActivationToken == token).ToListAsync();

        if (users != null && users.Count > 0)
            return await generateVerificationTokenAsync();

        return token;
    }

    public async Task<LoginResponse> GenerateJwtToken(string email, string applicationCode, ICollection<ApplicationRole> userRoles)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim("TenantCode", applicationCode),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add roles to claims
        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole.Name));
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"] ?? ""));

        var token = new JwtSecurityToken(
            issuer: _config["JWT:ValidIssuer"],
            audience: _config["JWT:ValidAudience"],

            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = token.ValidTo,
            Message = "Token generated successfully"
        };
    }

    public async Task<string> GenerateRefreshToken(string email, string SAPId)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"] ?? ""));

        var token = new JwtSecurityToken(
            issuer: _config["JWT:ValidIssuer"],
            audience: _config["JWT:ValidAudience"],

            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthClaims> GetUserClaim()
    {
        // Ensure HttpContext exists
        if (_httpContextAccessor.HttpContext == null)
        {
            return null; // Return null for background services
        }

        // Retrieve JWT token from Authorization header
        string jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(jwtToken) || !jwtToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null; // No valid token provided
        }

        try
        {
            jwtToken = jwtToken.Substring("Bearer ".Length).Trim();

            // Decode the JWT token
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            if (jsonToken != null)
            {
                // Extract claims
                var emailClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
                var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                var roles = _userManager.GetRolesAsync(_userManager.FindByIdAsync(userIdClaim).Result);
                return new AuthClaims
                {
                    Email = emailClaim,
                    Role = roles.Result.ToList(),
                    UserId = userIdClaim
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Invalid token: {ex.Message}"); // Log error instead of returning string
        }

        return null; // Return null on failure
    }

    public void SetUserClaims(HttpContext context, string userId, string role)
    {
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role ?? string.Empty)
        }, "firebase"));
    }

    public async Task<string> GetTokenAsync()
    {
        try
        {
            string jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last(); ;


            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                _logger.LogInformation("Missing token");
                return string.Empty;
            }

            return jwtToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(TokenService).Name, nameof(GetTokenAsync));
            return string.Empty;
        }
    }



}