using ConnectionManager;

namespace RaffleServicesProvider;

public static class RaffleHelperServices
{
    public static Result<char> DetectDilimited(string linea)
    {
        char[] posibles = new[] { ',', ';', '\t', '|' };
        var real = posibles.Select(d => new { Delimitador = d, Count = linea.Count(c => c == d) })
                            .OrderByDescending(x => x.Count)
                            .FirstOrDefault();
        if (real is null || real.Count == 0)
            return Result<char>.Failure("No se detectó un delimitador válido en la línea.");
        return Result<char>.Success(real.Delimitador);
    }
}
