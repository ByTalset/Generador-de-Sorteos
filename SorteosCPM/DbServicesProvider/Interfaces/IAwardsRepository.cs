using System.Data.Common;
using DbServicesProvider.Interfaces;

namespace DbServicesProvider;

public interface IAwardsRepository
{
    Task GenerateAreasAndAwardsAsync(string nombreTablaZonas, string nombreTablaAwards);
    Task<Dictionary<string, int>> InsertAreasAsync(List<Awards> premios, string nombreTabla);
    Task InsertAwardsAsync(List<Awards> premios, Dictionary<string, int> zonasId, string nombreTabla);
}
