namespace RaffleGenerator.Api.Dto;

public class RegisteredRequest
{
    public string NameRaffle { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}
