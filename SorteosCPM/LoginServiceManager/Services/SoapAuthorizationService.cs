using System.Security;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceManager;
using ServiceManager.Interfaces;

namespace LoginServiceManager;

public class SoapAuthorizationService : IAuthorization
{
    private readonly ILogger<SoapAuthorizationService> _logger;
    private readonly string _url;
    private readonly string _soapAction;
    private readonly int _idAplicacion;
    private readonly int _idSubAplicacion;
    private readonly List<string> _roles;
    public SoapAuthorizationService(IConfiguration configuration, ILogger<SoapAuthorizationService> logger)
    {
        // Constructor logic
        _logger = logger;
        _url = configuration.GetValue<string>("SoapAuth:Url") ?? string.Empty;
        _soapAction = configuration.GetValue<string>("SoapAuth:SoapAction") ?? string.Empty;
        _idAplicacion = configuration.GetValue<int>("SoapAuth:IdAplicacion");
        _idSubAplicacion = configuration.GetValue<int>("SoapAuth:IdSubAplicacion");
        _roles = configuration.GetSection("Roles").Get<List<string>>() ?? new List<string>();
    }

    public  Result<string> GetRole(string username, string password = "")
    {
        try
        {
            string url = _url;
            string soapAction = _soapAction;
            string envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
                                <s:Body>
                                    <Autenticar xmlns=""http://tempuri.org/"">
                                    <username>{SecurityElement.Escape(username)}</username>
                                    <idAplicacion>{_idAplicacion}</idAplicacion>
                                    <idSubAplicacion>{_idSubAplicacion}</idSubAplicacion>
                                    <password>{SecurityElement.Escape(password ?? string.Empty)}</password>
                                    </Autenticar>
                                </s:Body>
                                </s:Envelope>";
            string role = Task.Run(() => SendSoapRequest(url, soapAction, envelope)).GetAwaiter().GetResult();
            _logger.LogInformation("The following role {Role} is obtained.", role);
            return Result<string>.Success(role);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }

    private async Task<string> SendSoapRequest(string url, string soapAction, string envelope)
    {
        using HttpClient client = new();
        HttpRequestMessage request = new(HttpMethod.Post, url);
        request.Content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml");
        request.Headers.Add("SOAPAction", soapAction);
        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        string xml = await response.Content.ReadAsStringAsync();

        XDocument? doc = XDocument.Parse(xml);
        // Buscar idPerfil (case-insensitive)
        string idPerfilText = doc.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName == "IdPerfil", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        string idRole = _roles.FirstOrDefault(r => r == idPerfilText, "NoRole");
        return idRole;
    }
}
