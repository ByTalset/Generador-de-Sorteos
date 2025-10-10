using System.Data;
using DbServicesProvider.Dto;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace DbServicesProvider.Repositories.Sql;

public class SqlParticipantRepository : SqlBaseRepository, IParticipantRepository
{
    public SqlParticipantRepository(SqlConnection readConnection, SqlConnection writeConnection) :
    base(readConnection, writeConnection) { }

    public async Task GenerateParticipantAsync(string nombreTablaParticipantes, string nombreTablaZonas)
    {
        string query = QuerysHelper.CreateTableParticipant(nombreTablaParticipantes);
        using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Dictionary<string, int>> GetAreasAsync(string nombreTablaZonas)
    {
        Dictionary<string, int> areas = new();
        string query = QuerysHelper.GetAreas(nombreTablaZonas);
        using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            areas.Add(reader.GetString(1), reader.GetInt32(0));
        }
        return areas;
    }

    public async Task BulkInsertAsync(string nombreTabla, Guid processId, int idSorteo, IEnumerable<SqlDataRecord> participants)
    {
        await CreateProcessRecordAsync(processId, idSorteo);
        using SqlCommand command = new("dbo.InsertarParticipantes", _writeConnnection, _dbTransaction);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@nombreTabla", SqlDbType.NVarChar, 128) { Value = nombreTabla });
        command.Parameters.Add(new SqlParameter("@ProcessId", SqlDbType.UniqueIdentifier) { Value = processId });
        command.Parameters.Add(new SqlParameter("@Participantes", SqlDbType.Structured) { TypeName = "dbo.ParticipantesTipo", Value = participants });
        await command.ExecuteNonQueryAsync();
        string indexes = QuerysHelper.CreateIndexes(nombreTabla);
        using (SqlCommand cmd = new(indexes, _writeConnnection, _dbTransaction))
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task CreateProcessRecordAsync(Guid processId, int idSorteo)
    {
        using SqlCommand command = new("dbo.GestionarProcessCarga", _writeConnnection, _dbTransaction);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@Action", SqlDbType.NVarChar, 10) { Value = "CREATE" });
        command.Parameters.Add(new SqlParameter("@ProcessId", SqlDbType.UniqueIdentifier) { Value = processId });
        command.Parameters.Add(new SqlParameter("@IdSorteo", SqlDbType.Int) { Value = idSorteo });
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<int>> GetIdsParticipantsAsync(int idSorteo, int idZona)
    {
        List<int> idsZona = new();
        string nombreTablaParticipantes = $"{idSorteo}_Participantes";
        string query = QuerysHelper.GetPartcipantsForZona(nombreTablaParticipantes, idZona);
        using SqlCommand command = new(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            idsZona.Add(reader.GetInt32(0));
        }
        return idsZona;
    }

    public async Task<Participants> GetParticipantsWithoutWinningAsync(int idSorteo, int idZona, int folio)
    {
        Participants winner = new();
        string nombreTablaParticipantes = $"{idSorteo}_Participantes";
        string query = QuerysHelper.GetPartcipants(nombreTablaParticipantes, idZona, folio);
        using SqlCommand command = new(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            winner.IdParticipante = reader.GetInt32("IdParticipante");
            winner.Folio = reader.GetInt64("Folio");
            winner.CIF = reader.GetString("CIF");
            winner.Nombre = $"{reader.GetString("Nombre")} {reader.GetString("SegundoNombre")} {reader.GetString("PrimerApellido")}";
            winner.Telefono = await reader.IsDBNullAsync("Telefono") ? string.Empty : reader.GetString("Telefono");
            winner.Domicilio = await reader.IsDBNullAsync("Domicilio") ? string.Empty : reader.GetString("Domicilio");
            winner.Estado = await reader.IsDBNullAsync("Estado") ? string.Empty : reader.GetString("Estado");
            winner.Sucursal = await reader.IsDBNullAsync("Sucursal") ? string.Empty : reader.GetString("Sucursal");
            winner.Plaza = await reader.IsDBNullAsync("Plaza") ? string.Empty : reader.GetString("Plaza");
        }
        return winner;
    }
    public async Task<bool> InsertWinnerAsync(int IdParticipante, string cif, int idZona, int idPremio, int idSorteo)
    {
        string query = QuerysHelper.InsertWinner(IdParticipante, cif, idZona, idPremio, idSorteo);
        using SqlCommand command = new(query, _writeConnnection, _dbTransaction);
        int rowAffected = await command.ExecuteNonQueryAsync();
        return rowAffected > 0;
    }

    public async Task<List<Participants>> GetWinnersAsync(int idSorteo, int? idZona)
    {
        List<Participants> winner = new();
        string query = QuerysHelper.GetWinners(idSorteo, idZona);
        using SqlCommand command = new(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            winner.Add(new Participants
            {
                Descripcion = reader.GetString("Premio"),
                Folio = reader.GetInt64("Folio"),
                CIF = reader.GetString("CIF"),
                Nombre = reader.GetString("Nombre"),
                SegundoNombre = reader.GetString("SegundoNombre"),
                PrimerApellido = reader.GetString("PrimerApellido"),
                SegundoApellido = reader.GetString("SegundoApellido"),
                Telefono = await reader.IsDBNullAsync("Telefono") ? string.Empty : reader.GetString("Telefono"),
                Domicilio = await reader.IsDBNullAsync("Domicilio") ? string.Empty : reader.GetString("Domicilio"),
                Estado = await reader.IsDBNullAsync("Estado") ? string.Empty : reader.GetString("Estado"),
                Sucursal = await reader.IsDBNullAsync("Sucursal") ? string.Empty : reader.GetString("Sucursal"),
                Plaza = await reader.IsDBNullAsync("Plaza") ? string.Empty : reader.GetString("Plaza"),
                Zona = reader.GetString("NameZona")
            });
        }
        return winner;
    }

    public async Task<LoadFile> GetProcessAsync(int idSorteo, Guid processId)
    {
        LoadFile load = new();
        string query = QuerysHelper.GetProcees(idSorteo, processId);
        using SqlCommand command = new(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            load.ProcessId = await reader.IsDBNullAsync("ProcessId") ? Guid.Empty : reader.GetGuid("ProcessId");
            load.Status = Enum.Parse<LoadStatus>(reader.GetString("Estatus"));
            load.IdSorteo = reader.GetInt32("IdSorteo");
            load.CreatedAt = await reader.IsDBNullAsync("CreadoA") ? default : reader.GetDateTime("CreadoA");
            load.CompletedAt = await reader.IsDBNullAsync("CompletadoA") ? default : reader.GetDateTime("CompletadoA");
            load.RowsProcessed = await reader.IsDBNullAsync("FilasProcesadas") ? default : reader.GetInt32("FilasProcesadas");
            load.ErrorMessage = await reader.IsDBNullAsync("MensajeError") ? default : reader.GetString("MensajeError");
        }
        return load;
    }
}
