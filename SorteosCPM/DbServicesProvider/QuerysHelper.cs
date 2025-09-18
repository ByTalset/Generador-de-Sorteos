using System.Text;
using DbServicesProvider.Dto;

namespace DbServicesProvider;

public static class QuerysHelper
{
    public static string GetRaffles()
    {
        StringBuilder builder = new();
        builder.AppendLine("SELECT *     ");
        builder.Append("    FROM SorteosCPM ");
        return builder.ToString();
    }
    public static string GetRaffles(int idSorteo)
    {
        StringBuilder builder = new();
        builder.AppendLine("SELECT *                    ");
        builder.AppendLine("FROM SorteosCPM             ");
        builder.Append($"   WHERE IdSorteo = {idSorteo} ");
        return builder.ToString();
    }

    public static string GetAreas(string nombreTablaZonas)
    {
        StringBuilder builder = new();
        builder.AppendLine("SELECT *              ");
        builder.Append($"   FROM [{nombreTablaZonas}] ");
        return builder.ToString();
    }

    public static string InsertRaffle(string nombreSorteo, string? route)
    {
        StringBuilder builder = new();
        builder.AppendLine("INSERT INTO SorteosCPM (\"Nombre\", \"RutaImagen\")     ");
        builder.Append($"   VALUES ('{nombreSorteo}', '{route ?? string.Empty}') ");
        return builder.ToString();
    }

    public static string CreateTableAreas(string nombreTabla)
    {
        StringBuilder builder = new();
        builder.AppendLine($"CREATE TABLE [{nombreTabla}] (          ");
        builder.AppendLine("    IdZona INT PRIMARY KEY IDENTITY,     ");
        builder.AppendLine("    Nombre NVARCHAR(100) NOT NULL UNIQUE ");
        builder.AppendLine(")                                        ");
        return builder.ToString();
    }

    public static string CreateTableAwards(string nombreTabla)
    {
        StringBuilder builder = new();
        builder.AppendLine($"CREATE TABLE [{nombreTabla}] (         ");
        builder.AppendLine("    IdPremio INT PRIMARY KEY IDENTITY,  ");
        builder.AppendLine("    Descripcion NVARCHAR(100) NOT NULL, ");
        builder.AppendLine("    Cantidad INT NOT NULL,              ");
        builder.AppendLine($"   IdZona INT                          ");
        builder.AppendLine(")                                       ");
        return builder.ToString();
    }

    public static string CreateTableParticipant(string nombreTablaParticipants)
    {
        StringBuilder builder = new();
        builder.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('{nombreTablaParticipants}') AND type = 'U') ");
        builder.AppendLine("BEGIN                                                                                                      ");
        builder.AppendLine($"   CREATE TABLE [{nombreTablaParticipants}] (                                                                     ");
        builder.AppendLine("        IdParticipante INT PRIMARY KEY IDENTITY,                                                           ");
        builder.AppendLine("        Folio BIGINT NOT NULL,                                                                             ");
        builder.AppendLine("        CIF NVARCHAR(100) NOT NULL,                                                                        ");
        builder.AppendLine("        Nombre NVARCHAR(100) NOT NULL,                                                                     ");
        builder.AppendLine("        SegundoNombre NVARCHAR(100) NOT NULL,                                                              ");
        builder.AppendLine("        PrimerApellido NVARCHAR(100) NOT NULL,                                                             ");
        builder.AppendLine("        SegundoApellido NVARCHAR(100) NOT NULL,                                                            ");
        builder.AppendLine("        Telefono NVARCHAR(20),                                                                             ");
        builder.AppendLine("        Domicilio NVARCHAR(100),                                                                           ");
        builder.AppendLine("        Estado NVARCHAR(50),                                                                               ");
        builder.AppendLine("        Plaza NVARCHAR(50),                                                                                ");
        builder.AppendLine($"       IdZona INT                                                                                         ");
        builder.AppendLine("    )                                                                                                      ");
        builder.AppendLine("END                                                                                                        ");
        return builder.ToString();
    }

    public static string InsertAreas(string nombreTabla, string zona)
    {
        StringBuilder builder = new();
        builder.AppendLine($"INSERT INTO [{nombreTabla}] (Nombre) OUTPUT INSERTED.IdZona ");
        builder.Append($"    VALUES ('{zona}')                                             ");
        return builder.ToString();
    }

    public static string InsertAwards(string nombreTabla, int cantidad, string descripcion, int idZona)
    {
        StringBuilder builder = new();
        builder.AppendLine($"INSERT INTO [{nombreTabla}] (Cantidad, Descripcion, IdZona) ");
        builder.Append($"    VALUES ({cantidad}, '{descripcion}', {idZona})              ");
        return builder.ToString();
    }

    public static string UpdateRaffle(int idSorteo, string nombreTabla)
    {
        StringBuilder builder = new();
        builder.AppendLine(" UPDATE [SorteosCPM]                   ");
        builder.AppendLine($"SET [Tabla_Zonas] = '{nombreTabla}'   ");
        builder.Append($"    WHERE IdSorteo = {idSorteo}           ");
        return builder.ToString();
    }

    public static string GetAreasAndArwards(string nombreTablaZonas, string nombreTablaPremios)
    {
        string query = $@";WITH PremiosDisponibles AS (
                                SELECT TOP 1
                                    z.IdZona,
                                    z.Nombre,
                                    p.IdPremio,
                                    p.Descripcion
                                    --p.Cantidad - COUNT(g.IdGanador) AS CantidadRestante,
                                    --p.Cantidad AS Total
                                FROM [1_Premios] p
                                INNER JOIN [1_Zonas] z ON p.IdZona = z.IdZona
                                LEFT JOIN Ganadores g ON p.IdPremio = g.IdPremio AND g.IdSorteo = 1
                                GROUP BY z.IdZona, z.Nombre, p.IdPremio, p.Descripcion, p.Cantidad
                                HAVING p.Cantidad - COUNT(g.IdGanador) > 0
                                ORDER BY z.IdZona, p.IdPremio
                            )
                            SELECT TOP 1
                                (SELECT COUNT(*) FROM Ganadores WHERE IdSorteo = 1) + 1 AS NumeroPremio,
                                d.IdZona,
                                d.Nombre,
                                d.IdPremio,
                                d.Descripcion
                            FROM PremiosDisponibles d
                            ORDER BY d.IdZona, d.IdPremio;";
        return query;
    }

    public static string GetPartcipantsForZona(string nombreTablaParticipantes, int idZona)
    {
        StringBuilder builder = new();
        builder.AppendLine(" SELECT IdParticipante                                     ");
        builder.AppendLine($"FROM [{nombreTablaParticipantes}]                         ");
        builder.Append($"    WHERE idZona = {idZona}                                   ");
        return builder.ToString();
    }
    public static string GetPartcipants(string nombreTablaParticipantes, int idZona, int folio)
    {
        StringBuilder builder = new();
        builder.AppendLine(" SELECT *                                                  ");
        builder.AppendLine($"FROM [{nombreTablaParticipantes}] AS P                    ");
        builder.AppendLine($"WHERE IdParticipante = {folio}                            ");
        builder.AppendLine($"AND idZona = {idZona}                                     ");
        builder.AppendLine("AND NOT EXISTS (SELECT *                                   ");
        builder.AppendLine("                FROM Ganadores AS G                        ");
        builder.Append("                    WHERE G.CIF = P.CIF)                       ");
        return builder.ToString();
    }
    public static string InsertWinner(int IdParticipante, string cif, int idZona, int idPremio, int idSorteo)
    {
        StringBuilder builder = new();
        builder.AppendLine($"INSERT INTO [Ganadores] (IdParticipante, CIF, IdZona, IdPremio, IdSorteo) ");
        builder.Append($"    VALUES ({IdParticipante}, '{cif}', {idZona}, {idPremio}, {idSorteo})          ");
        return builder.ToString();
    }

    public static string GetWinners(int idSorteo, int? idZona)
    {
        StringBuilder builder = new();
        builder.AppendLine(" SELECT	R.Descripcion AS \"Premio\", P.Folio, P.CIF, P.Nombre, P.SegundoNombre, P.PrimerApellido, P.SegundoApellido, P.Telefono, P.Domicilio, P.Estado, P.Plaza, Z.Nombre as NameZona ");
        builder.AppendLine($"FROM [Ganadores] AS G                                                                                 ");
        builder.AppendLine($"JOIN [{idSorteo}_Participantes] AS P ON P.IdParticipante = G.IdParticipante                           ");
        builder.AppendLine($"JOIN [{idSorteo}_Premios] AS R ON R.IdPremio = G.IdPremio                                             ");
        builder.AppendLine($"JOIN [{idSorteo}_Zonas] AS Z ON Z.IdZona = G.IdZona                                                   ");
        if (idZona > 0)
            builder.Append($"WHERE Z.IdZona = {idZona}                                                   ");
        return builder.ToString();
    }

    public static string CreateIndexes(string nombreTabla)
    {
        string slqIndices = $@"IF NOT EXISTS(
                                 SELECT 1 FROM sys.indexes
                                 WHERE name = 'IX_{nombreTabla}_IdZona'
                                 AND object_id = OBJECT_ID('{nombreTabla}')
                               )
                               BEGIN
                                    CREATE NONCLUSTERED INDEX IX_{nombreTabla}_IdZona
                                    ON [{nombreTabla}] (IdZona)
                               END
                               
                               IF NOT EXISTS(
                                 SELECT 1 FROM sys.indexes
                                 WHERE name = 'IX_{nombreTabla}_IdZona_Id'
                                 AND object_id = OBJECT_ID('{nombreTabla}')
                               )
                               BEGIN
                                    CREATE NONCLUSTERED INDEX IX_{nombreTabla}_IdZona_Id
                                    ON [{nombreTabla}] (IdZona, IdParticipante)
                               END
                               
                               IF NOT EXISTS(
                                 SELECT 1 FROM sys.indexes
                                 WHERE name = 'IX_{nombreTabla}_IdZona_Cover'
                                 AND object_id = OBJECT_ID('{nombreTabla}')
                               )
                               BEGIN
                                    CREATE NONCLUSTERED INDEX IX_{nombreTabla}_IdZona_Cover
                                    ON [{nombreTabla}] (IdZona, IdParticipante) INCLUDE (Folio, CIF, Nombre, Telefono)
                               END";

        return slqIndices;
    }

    public static string DeleteRaffle(int idSorteo)
    {
        StringBuilder builder = new();
        builder.AppendLine($"DELETE FROM SorteosCPM      ");
        builder.Append($"    WHERE IdSorteo = {idSorteo} ");
        return builder.ToString();
    }

    public static string ResetRaffle(int idSorteo)
    {
        StringBuilder builder = new();
        builder.AppendLine($"DELETE FROM Ganadores");
        builder.Append($"    WHERE IdSorteo = {idSorteo}");
        return builder.ToString();
    }

    public static string UpdateRaffle(int idSorteo, string? nombreSorteo, string? routeImage, string? permiso)
    {
        var setters = new List<string>();
        if (!string.IsNullOrEmpty(nombreSorteo)) setters.Add($"[Nombre] = '{nombreSorteo}' ");
        if (!string.IsNullOrEmpty(permiso)) setters.Add($"[PermisoSegob] = '{permiso}' ");
        if (!string.IsNullOrEmpty(routeImage)) setters.Add($"[RutaImagen] = '{routeImage}' ");
        if (setters.Count == 0) throw new ArgumentException("No hay campos para actualizar");
        string query = $@"UPDATE SorteosCPM
                          SET {string.Join(",", setters)}
                          WHERE IdSorteo = {idSorteo} ";
        return query;
    }

    public static string GetRouteImage(int idSorteo)
    {
        StringBuilder builder = new();
        builder.AppendLine(" SELECT [RutaImagen]                    ");
        builder.AppendLine($"FROM SorteosCPM                        ");
        builder.Append($"    WHERE [IdSorteo] = {idSorteo}          ");
        return builder.ToString();
    }

    public static string RegisterProcees(LoadFile load)
    {
        string query = $@"INSERT INTO ProcessCarga(ProcessId, IdSorteo, Estatus, CreadoA)
                          VALUES ('{load.ProcessId}', {load.IdSorteo}, '{load.Status}', '{load.CreatedAt.ToUniversalTime()}')";
        return query;
    }

    public static string UpdateProcees(LoadFile load)
    {
        var setters = new List<string>();
        setters.Add($"[Estatus] = '{load.Status}' ");
        setters.Add($"[FilasProcesadas] = {load.RowsProcessed} ");
        if (load.CompletedAt != null) setters.Add($"[CompletadoA] = '{load.CompletedAt}' ");
        if (!string.IsNullOrEmpty(load.ErrorMessage)) setters.Add($"[MensajeError] = '{load.ErrorMessage}' ");
        string query = $@"UPDATE ProcessCarga
                          SET {string.Join(",", setters)}
                          WHERE ProcessId = {load.ProcessId}";
        return query;
    }

    public static string GetProcees(int idSorteo, Guid processId)
    {
        StringBuilder builder = new();
        builder.AppendLine(" SELECT * ");
        builder.AppendLine(" FROM ProcessCarga WITH (NOLOCK)");
        builder.AppendLine($"WHERE IdSorteo = {idSorteo}");
        builder.Append($"    AND ProcessId = '{processId}' ");
        return builder.ToString();
    }
}  
