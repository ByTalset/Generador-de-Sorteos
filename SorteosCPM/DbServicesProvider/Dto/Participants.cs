namespace DbServicesProvider.Dto;

public class Participants
{
    public int IdParticipante { get; set; }
    public long Folio { get; set; }
    public string CIF { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string SegundoNombre { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string SegundoApellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Domicilio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int Plaza { get; set; }
    public string NameZona { get; set; } = string.Empty;
    public string Premio { get; set; } = string.Empty;
}
