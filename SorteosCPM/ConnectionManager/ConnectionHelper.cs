using Microsoft.Extensions.Logging;

namespace ConnectionManager;

public class ConnectionHelper
{
    protected ConnectionHelper() { }
    public static Result<string> GetConnectionString(ILogger logger, string nameVariable)
    {
        if (string.IsNullOrEmpty(nameVariable))
        {
            logger.LogInformation("The name of the environment variable in appsettings.json is empty.");
            return Result<string>.Failure("The variable name cannot be empty.");
        }
        if (Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine).Contains(nameVariable))
        {
            string? connectionString = Environment.GetEnvironmentVariable(nameVariable, EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogInformation("The environment variable '{NameVariable}' is empty or does not exist.", nameVariable);
                return Result<string>.Failure($"The environment variable is empty or does not exist.");
            }
            return Result<string>.Success(connectionString);
        }
        logger.LogInformation("The environment variable in appsettings.json '{NameVariable}' does not exist.", nameVariable);
        return Result<string>.Failure($"The environment variable does not exist.");
    }
}