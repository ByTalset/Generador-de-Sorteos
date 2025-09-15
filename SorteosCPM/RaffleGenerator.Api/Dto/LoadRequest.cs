namespace RaffleGenerator.Api.Dto;

public class LoadRequest
{
    public int IdSorteo { get; set; }
    public IFormFile? File { get; set; }
}
