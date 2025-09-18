using System.Data;
using DbServicesProvider.Dto;
using DbServicesProvider.Interfaces;
using Microsoft.Data.SqlClient;

namespace DbServicesProvider.Repositories.Sql;

public class SqlRaffleRepository : SqlBaseRepository, IRaffleRepository
{
    public SqlRaffleRepository(SqlConnection readConnection, SqlConnection writeConnection) :
    base(readConnection, writeConnection) { }

    public async Task<string> GetRaffleAsync(int idSorteo)
    {
        string query = QuerysHelper.GetRaffles(idSorteo);
        using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return await reader.IsDBNullAsync("Tabla_Zonas") ? string.Empty : reader.GetString("Tabla_Zonas");
        }
        return string.Empty;
    }

    public async Task<List<Raffle>> GetAllAsync()
    {
        List<Raffle> raffles = new();
        string query = QuerysHelper.GetRaffles();
        using SqlCommand command = new(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            raffles.Add(new Raffle
            {
                IdSorteo = reader.GetInt32("IdSorteo"),
                NombreSorteo = reader.GetString("Nombre"),
                RutaImagen = reader.GetString("RutaImagen")
            });
        }
        return raffles;
    }

    public async Task<bool> InsertAsync(string nombreSorteo, string? route)
    {
        string query = QuerysHelper.InsertRaffle(nombreSorteo, route);
        using SqlCommand command = new(query, _writeConnnection, _dbTransaction);
        int rowAffected = await command.ExecuteNonQueryAsync();
        return rowAffected > 0;
    }

    public async Task UpdateRaffleAsync(int idSorteo, string nombreTabla)
    {
        string query = QuerysHelper.UpdateRaffle(idSorteo, nombreTabla);
        using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<Awards> GetAreasAndAwardsAsync(int idSorteo)
    {
        Awards premios = new();
        string nombreTablaZonas = $"{idSorteo}_Zonas";
        string nombreTablaPremios = $"{idSorteo}_Premios";
        string query = QuerysHelper.GetAreasAndArwards(idSorteo, nombreTablaZonas, nombreTablaPremios);
        using SqlCommand command = new(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            premios.NumPremio = reader.GetInt32(0);
            premios.IdZona = reader.GetInt32(1);
            premios.Zona = reader.GetString(2);
            premios.IdPremio = reader.GetInt32(3);
            premios.Descripcion = reader.GetString(4);
        }

        return premios;
    }

    public async Task<List<Zona>> GetAreasAllAsync(int idSorteo)
    {
        List<Zona> zonas = new();
        string nombreTablaZonas = $"{idSorteo}_Zonas";
        string query = QuerysHelper.GetAreas(nombreTablaZonas);
        using SqlCommand command = new(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var zona = new Zona()
            {
                IdZona = reader.GetInt32(0),
                Nombre = reader.GetString(1)
            };
            zonas.Add(zona);
        }

        return zonas;
    }

    public async Task<bool> DeleteRaffleAsync(int idSorteo)
    {
        string query = QuerysHelper.DeleteRaffle(idSorteo);
        using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
        int rowAffected = await command.ExecuteNonQueryAsync();
        return rowAffected > 0;
    }

    public async Task<bool> ResetRaffleAsync(int idSorteo)
    {
        string query = QuerysHelper.ResetRaffle(idSorteo);
        using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
        int rowAffected = await command.ExecuteNonQueryAsync();
        return rowAffected > 0;
    }

    public async Task<string> GetRouteImageAsync(int idSorteo)
    {
        string query = QuerysHelper.GetRouteImage(idSorteo);
        using var command = new SqlCommand(query, _readConnection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        string routeImage = await reader.ReadAsync() ? reader.GetString("RutaImagen") : string.Empty;
        return routeImage;
    }

    public async Task<bool> UpdateRaffleAsync(int idSorteo, string? nombreSorteo, string? permiso, string? routeImage)
    {
        string query = QuerysHelper.UpdateRaffle(idSorteo, nombreSorteo, routeImage, permiso);
        using var command = new SqlCommand(query, _writeConnnection, _dbTransaction);
        await command.ExecuteNonQueryAsync();
        int rowAffected = await command.ExecuteNonQueryAsync();
        return rowAffected > 0;
    }
}
