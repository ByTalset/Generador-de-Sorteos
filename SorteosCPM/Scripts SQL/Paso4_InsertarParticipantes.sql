CREATE PROCEDURE dbo.InsertarParticipantes
	@nombreTabla NVARCHAR(128),
	@Participantes dbo.ParticipantesTipo READONLY,
	@ProcessId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;
	BEGIN TRY
		EXEC dbo.GestionarProcessCarga
			@Action = 'UPDATE', 
			@ProcessId = @ProcessId, 
			@Estatus = 'Processing'

		-- Declarar una variable para almacenar la consulta SQL.
		DECLARE @sql NVARCHAR(MAX);
		DECLARE @rowCount INT;
	
		-- Construir la consulta dinámicamente.
		-- Usar QUOTENAME para proteger el nombre de la tabla de inyección de SQL.
		SET @sql = N'INSERT INTO ' + QUOTENAME(@nombreTabla) + '(Folio, CIF, Nombre, SegundoNombre, PrimerApellido, SegundoApellido, Telefono, Domicilio, Estado, Plaza, IdZona)
						SELECT Folio, CIF, Nombre, SegundoNombre, PrimerApellido, SegundoApellido, Telefono, Domicilio, Estado, Plaza, IdZona
						FROM @Participantes;';

		-- Ejecutar la consulta dinámica.
		-- Se pasa el nombre del tipo de tabla y la variable de la tabla para que sp_executesql pueda leerla.
		EXEC sp_executesql @sql, N'@Participantes dbo.ParticipantesTipo READONLY', @Participantes;

		SET @rowCount = @@ROWCOUNT

		EXEC dbo.GestionarProcessCarga
			@Action = 'UPDATE', 
			@ProcessId = @ProcessId, 
			@Estatus = 'Completed',
			@FilasProcesadas = @rowCount
	END TRY
	BEGIN CATCH
		EXEC dbo.GestionarProcessCarga 
			@Action = 'UPDATE',
			@ProcessId = @ProcessId,
			@Estatus = 'Failed',
			@MensajeError = ERROR_MESSAGE;
		THROW;
	END CATCH
END