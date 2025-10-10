namespace DbServicesProvider.Dto;

public class Participants : Awards
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
    public string Sucursal { get; set; } = string.Empty;
    public string Plaza { get; set; } = string.Empty;
}
