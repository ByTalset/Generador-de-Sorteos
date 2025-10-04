using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ServiceManager.Services;

public class JwtAuthorizationService
{
    private readonly ILogger<JwtAuthorizationService> _logger;
    private readonly string _secretKey;
    private readonly int _tokenExpiryTime;
    private readonly string _timeType;
    public JwtAuthorizationService(IConfiguration configuration, ILogger<JwtAuthorizationService> logger)
    {
        _logger = logger;
        _secretKey = configuration.GetSection("Jwt:Key").Value ?? "";
        _tokenExpiryTime = int.TryParse(configuration.GetSection("Jwt:ExpiryTime").Value, out int expiryTime) ? expiryTime : 5; // Default to 5 minutes if not set
        _timeType = configuration.GetSection("Jwt:TimeType").Value ?? "Minutes"; // Default to Minutes if not set
    }

    // Additional methods for generating and validating JWT tokens can be added here
    public Result<string> GenerateJwt(string username, string role)
    {
        if(IsNullOrEmpty(result: out Result<bool> isNull))
            return Result<string>.Failure(isNull.Error);
        Result<DateTime> expiryTime = GetExpiryTime();
        if (!expiryTime.IsSuccess)
            return Result<string>.Failure(expiryTime.Error);

        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = System.Text.Encoding.UTF8.GetBytes(_secretKey);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = expiryTime.Value,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        _logger.LogInformation("The JWT Token is obtained.");
        return Result<string>.Success(tokenHandler.WriteToken(token));
    }

    public Result<ClaimsPrincipal> ValidateJwt(string accessToken)
    {
        if (IsNullOrEmpty(result: out Result<bool> isNull))
            return Result<ClaimsPrincipal>.Failure(isNull.Error);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role
        };

        JwtSecurityTokenHandler tokenHandler = new();
        ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(accessToken, validationParameters, out SecurityToken validatedToken);
        if (validatedToken is not JwtSecurityToken jwtToken || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCulture))
            return Result<ClaimsPrincipal>.Failure("Error verifying the security of the Jwt token");
        _logger.LogInformation("The JWT Token is validated.");
        return Result<ClaimsPrincipal>.Success(claimsPrincipal);
    }

    private Result<DateTime> GetExpiryTime()
    {
        Result<DateTime> expiryTime = _timeType.ToLower() switch
        {
            "seconds" => Result<DateTime>.Success(DateTime.UtcNow.AddSeconds(_tokenExpiryTime)),
            "minutes" => Result<DateTime>.Success(DateTime.UtcNow.AddMinutes(_tokenExpiryTime)),
            "hours" => Result<DateTime>.Success(DateTime.UtcNow.AddHours(_tokenExpiryTime)),
            "days" => Result<DateTime>.Success(DateTime.UtcNow.AddDays(_tokenExpiryTime)),
            _ => Result<DateTime>.Failure("Invalid TimeType in configuration. Use 'Seconds', 'Minutes', 'Hours', or 'Days'.")
        };
        return expiryTime;
    }

    private bool IsNullOrEmpty(out Result<bool> result)
    {
        result = Result<bool>.Success(true);
        if (_secretKey.IsNullOrEmpty())
            result = Result<bool>.Failure("Jwt:Key is not configured in appsettings.json");
        return _secretKey.Length == 0;
    }
}