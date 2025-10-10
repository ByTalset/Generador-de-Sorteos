using System.Security.Claims;
using ServiceManager.Interfaces;
using ServiceManager.Services;

namespace ServiceManager;

public class LoginManagement
{
    private readonly IAuthentication _authenticationService;
    private readonly IAuthorization _authorizationService;
    private readonly JwtAuthorizationService _jwtAuthentication;
    public LoginManagement(IAuthentication authenticationService, IAuthorization authorizationService, JwtAuthorizationService jwtAuthentication)
    {
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
        _jwtAuthentication = jwtAuthentication;
    }

    public Result<string> Login(string username, string password)
    {
        Result<bool> authenticated = _authenticationService.AutenticatedUser(username, password);
        if (!authenticated.IsSuccess) return Result<string>.Failure(authenticated.Error);
        if (authenticated.Value)
        {
            Result<string> authorizate = _authorizationService.GetRole(username);
            if (!authorizate.IsSuccess) return Result<string>.Failure(authorizate.Error);
            if (authorizate.Value != "NoRole")
                return _jwtAuthentication.GenerateJwt(username, authorizate.Value);
            return Result<string>.Failure("User has no assigned role", 401);
        }
        return Result<string>.Failure("Invalid credentials");
    }
    
    public Result<string> RenewToken(string token)
    {
        Result<ClaimsPrincipal> validatedToken = _jwtAuthentication.ValidateJwt(token);
        if (!validatedToken.IsSuccess) return Result<string>.Failure(validatedToken.Error);
        string? username = validatedToken.Value.Identity?.Name;
        string? role = validatedToken.Value.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role)) return Result<string>.Failure("Invalid token", codeError: 401);
        return _jwtAuthentication.GenerateJwt(username, role);
    }
}
