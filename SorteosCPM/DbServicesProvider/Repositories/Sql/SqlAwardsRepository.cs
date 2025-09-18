using System.Data;
using Microsoft.Data.SqlClient;

namespace DbServicesProvider.Repositories.Sql;

public class SqlAwardsRepository : SqlBaseRepository, IAwardsRepository
{
    public SqlAwardsRepository(SqlConnection readConnection, SqlConnection writeConnection) :
    base(readConnection, writeConnection) { }

    public async Task GenerateAreasAndAwardsAsync(string nombreTablaZonas, string nombreTablaAwards)
    {
        string queryZonas = QuerysHelper.CreateTableAreas(nombreTablaZonas);
        using SqlCommand commandZonas = new(queryZonas, _writeConnnection, _dbTransaction);
        await commandZonas.ExecuteNonQueryAsync();
        string queryAwards = QuerysHelper.CreateTableAwards(nombreTablaAwards);
        using SqlCommand commandAwards = new(queryAwards, _writeConnnection, _dbTransaction);
        await commandAwards.ExecuteNonQueryAsync();
    }

    public async Task<Dictionary<string, int>> InsertAreasAsync(List<Awards> premios, string nombreTabla)
    {
        IEnumerable<string> zonas = premios.Select(p => p.Zona.Trim('"')).Distinct().ToList();
        Dictionary<string, int> zonasId = new();
        foreach (string zona in zonas)
        {
            string query = QuerysHelper.InsertAreas(nombreTabla, zona);
            using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
            var idZona = await command.ExecuteScalarAsync();
            zonasId[zona] = Convert.ToInt32(idZona);
        }
        return zonasId;
    }

    public async Task InsertAwardsAsync(List<Awards> premios, Dictionary<string, int> zonasId, string nombreTabla)
    {
        foreach (Awards premio in premios)
        {
            int idZona = zonasId[premio.Zona];
            string query = QuerysHelper.InsertAwards(nombreTabla, premio.Cantidad, premio.Descripcion, idZona);
            using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
            await command.ExecuteNonQueryAsync();
        }
    }
}
