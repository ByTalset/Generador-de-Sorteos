using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ServiceManager.Interfaces;

namespace ServiceManager.Services;

public class ADAuthenticationServices : IAuthentication
{
    private readonly ILogger<ADAuthenticationServices> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _domain;
    public ADAuthenticationServices(IConfiguration configuration, ILogger<ADAuthenticationServices> logger)
    {
        // Constructor logic
        _logger = logger;
        _configuration = configuration;
        _domain = _configuration.GetSection("AD:Domain").Value ?? "";
    }

    // Implementation of authentication services would go here
    public Result<bool> AutenticatedUser(string username, string password)
    {
        if (_domain.IsNullOrEmpty()) return Result<bool>.Failure("AD:Domain is not configured in appsettings.json");
        using PrincipalContext context = new PrincipalContext(ContextType.Domain, _domain);
        _logger.LogInformation("The following credentials are validated:\nUsername:{user}\nPassword:{pass}.", username, password);
        return Result<bool>.Success(context.ValidateCredentials(username, password));
    }
}
