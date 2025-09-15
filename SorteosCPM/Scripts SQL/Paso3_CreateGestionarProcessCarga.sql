CREATE PROCEDURE dbo.GestionarProcessCarga
	@Action NVARCHAR(10), -- 'CREATE', 'UPDATE'
	@ProcessId UNIQUEIDENTIFIER = NULL,
	@IdSorteo INT = NULL,
	@Estatus NVARCHAR(50) = NULL,
	@FilasProcesadas INT = NULL,
	@MensajeError NVARCHAR(MAX) = NULL
AS
BEGIN
	SET NOCOUNT ON;
	IF @Action = 'CREATE'
	BEGIN
		SET @ProcessId = ISNULL(@ProcessId, NEWID())

		INSERT INTO ProcessCarga (ProcessId, IdSorteo, Estatus, CreadoA)
		VALUES (@ProcessId, @IdSorteo, 'Pending', SYSUTCDATETIME())
	END
	ELSE IF @Action = 'UPDATE'
	BEGIN
		UPDATE ProcessCarga
		SET Estatus = @Estatus,
			CompletadoA = CASE WHEN @Estatus IN ('Completed', 'Failed') THEN SYSUTCDATETIME() ELSE CompletadoA END,
			FilasProcesadas = COALESCE(@FilasProcesadas, FilasProcesadas),
			MensajeError = COALESCE(@MensajeError, MensajeError)
		WHERE ProcessId = @ProcessId
	END
END