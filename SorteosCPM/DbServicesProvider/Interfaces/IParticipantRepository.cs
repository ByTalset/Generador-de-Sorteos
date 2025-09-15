using DbServicesProvider.Dto;
using Microsoft.Data.SqlClient.Server;
namespace DbServicesProvider;

public interface IParticipantRepository
{
    Task GenerateParticipantAsync(string nombreTablaParticipantes, string nombreTablaZonas);
    Task<Dictionary<string, int>> GetAreasAsync(string nombreTablaZonas);
    Task BulkInsertAsync(string nombreTabla, Guid processId, int idSorteo, IEnumerable<SqlDataRecord> participants);
    Task CreateProcessRecordAsync(Guid processId, int idSorteo);
    Task<List<int>> GetIdsParticipantsAsync(int idSorteo, int idZona);
    Task<Participants> GetParticipantsWithoutWinningAsync(int idSorteo, int idZona, int folio);
    Task<bool> InsertWinnerAsync(int IdParticipante, string cif, int idZona, int idPremio, int idSorteo);
    Task<List<Participants>> GetWinnersAsync(int idSorteo, int? idZona);
    Task<LoadFile> GetProcessAsync(int idSorteo, Guid processId);
}
