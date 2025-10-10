using DbServicesProvider.Dto;

namespace DbServicesProvider.Interfaces;

public interface IRaffleRepository
{
    Task<string> GetRaffleAsync(int idSorteo);
    Task<List<Raffle>> GetAllAsync();
    Task<bool> InsertAsync(string nombreSorteo, string? route);
    Task UpdateRaffleAsync(int idSorteo, string nombreTabla);
    Task<bool> UpdateRaffleAsync(int idSorteo, string? nombreSorteo, string? permiso, string? routeImage);
    Task<Awards> GetAreasAndAwardsAsync(int idSorteo);
    Task<List<Awards>> GetAwardsAllAsync(int idSorteo);
    Task<List<Zona>> GetAreasAllAsync(int idSorteo);
    Task<bool> DeleteRaffleAsync(int idSorteo);
    Task<bool> ResetRaffleAsync(int idSorteo);
    Task<string> GetRouteImageAsync(int idSorteo);
}
