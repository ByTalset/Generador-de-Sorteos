namespace DbServicesProvider;

public class Awards
{
    public int IdPremio { get; set; }
    public int NumPremio { get; set; }
    public int Cantidad { get; set; }
    public int IdZona { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
}
