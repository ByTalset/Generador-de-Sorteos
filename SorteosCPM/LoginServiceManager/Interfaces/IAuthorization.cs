namespace ServiceManager.Interfaces;

public interface IAuthorization
{
    Result<string> GetRole(string username, string password = "");
}
