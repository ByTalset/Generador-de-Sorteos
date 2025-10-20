using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ServiceManager.Interfaces;
using System.DirectoryServices.AccountManagement;

namespace ServiceManager.Services;

public class ADAuthorizationService : IAuthorization
{
    private readonly ILogger<ADAuthorizationService> _logger;
    private readonly string _domain;
    private readonly List<string> _roles;
    public ADAuthorizationService(IConfiguration configuration, ILogger<ADAuthorizationService> logger)
    {
        // Constructor logic
        _logger = logger;
        _domain = configuration.GetSection("AD:Domain").Value ?? "";
        _roles = configuration.GetSection("Roles").Get<List<string>>() ?? new List<string>();
    }
    // Implementation of authorization service would go here 
    public Result<string> GetRole(string username, string password = "")
    {
        if (_domain.IsNullOrEmpty()) return Result<string>.Failure("AD:Domain is not configured in appsettings.json");
        using PrincipalContext context = new PrincipalContext(ContextType.Domain, _domain);
        using UserPrincipal user = UserPrincipal.FindByIdentity(context, username);
        if (user == null)
            return Result<string>.Failure("User not found in Active Directory");

        List<string> groups = user.GetAuthorizationGroups()
                                .Where(g => g.SamAccountName != null)
                                .Select(g => g.SamAccountName)
                                .ToList();
        string role = groups.FirstOrDefault(g => _roles.Exists(r => r.Equals(g, StringComparison.OrdinalIgnoreCase))) ?? "NoRole";
        _logger.LogInformation("The following role {Role} is obtained.", role);
        return Result<string>.Success(role);
    }
}
