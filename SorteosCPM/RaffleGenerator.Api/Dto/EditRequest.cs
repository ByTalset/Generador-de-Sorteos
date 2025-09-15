namespace RaffleGenerator.Api;

public class EditRequest
{
    public int IdSorteo { get; set; }
    public string? NombreSorteo { get; set; }
    public string? PermisoSegob { get; set; }
    public IFormFile? Image { get; set; }
}
