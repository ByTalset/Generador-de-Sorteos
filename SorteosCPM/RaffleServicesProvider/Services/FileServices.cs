using System.Text;
using ConnectionManager;
using DbServicesProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RaffleServicesProvider.Interfaces;

namespace RaffleServicesProvider.Services;

public class FileServices : IFileGenerator
{
    private readonly ILogger<FileServices> _logger;
    private readonly string _pathTemp;
    public FileServices(IConfiguration configuration, ILogger<FileServices> logger)
    {
        _logger = logger;
        _pathTemp = Path.Combine(configuration.GetSection("Directory:PathTemp").Value ?? "C:\\SorteosCPM");
        if (!Directory.Exists(_pathTemp))
            Directory.CreateDirectory(_pathTemp);
    }
    public Task<Result<List<Awards>>> LoadPrizesAreasAsync(FileDto file)
    {
        Result<List<Awards>> premios = new Result<List<Awards>>(new(), false, string.Empty, default);
        try
        {   // 1. Convertir bytes a texto
            string contenido = Encoding.UTF8.GetString(file.Contenido);
            // 2. Separar en líneas
            string[] lineas = contenido.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lineas.Length <= 1)
            {
                premios.Error = "The file content is empty.";
                return Task.FromResult(premios);
            }
            var delimitador = RaffleHelperServices.DetectDilimited(lineas[0]);
            if (!delimitador.IsSuccess)
            {
                premios.Error = delimitador.Error;
                return Task.FromResult(premios);
            }

            for (int i = 1; i < lineas.Length; i++)
            {   // 3. Dividir cada línea por el delimitador
                string[] filas = lineas[i].Split(delimitador.Value);
                premios.Value.Add(new Awards
                {
                    Cantidad = int.TryParse(filas[0], out int result) ? result : 0,
                    Descripcion = filas[1],
                    Zona = filas[2]
                });
            }
            premios.IsSuccess = true;
            _logger.LogInformation("It is obtained {@premios} from the response.", premios.Value);
            return Task.FromResult(premios);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Task.FromResult(Result<List<Awards>>.Failure(ex.Message));
        }
    }

    public async Task<Result<string>> SaveFileTemp(FileDto file)
    {
        try
        {
            var route = Path.Combine(_pathTemp, $"{file.FileName}{file.Extension}");
            await File.WriteAllBytesAsync(route, file.Contenido);
            _logger.LogInformation("It is obtained {route} from the response.", route);
            return Result<string>.Success(route);
        }
        catch (Exception ex)
        {
            _logger.LogError("The following unexpected error occurred:{ex.Message}\n{ex.StackTrace}", ex.Message, ex.StackTrace);
            return Result<string>.Failure(ex.Message);
        }
    }
}
