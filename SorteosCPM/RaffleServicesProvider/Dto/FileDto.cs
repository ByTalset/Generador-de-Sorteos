namespace RaffleServicesProvider;

public class FileDto
{
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
}
