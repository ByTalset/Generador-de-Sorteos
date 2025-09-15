namespace ServiceManager.Interfaces;

public interface IAuthentication
{
    // Define methods for authentication here
    Result<bool> AutenticatedUser(string username, string password);
}
