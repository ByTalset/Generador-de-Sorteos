namespace DbServicesProvider.Dto;

public class LoadFile
{
    public int IdSorteo { get; set; }
    public Guid ProcessId { get; set; }
    public string Path { get; set; } = string.Empty;
    public LoadStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RowsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
}
