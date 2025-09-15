CREATE TYPE dbo.ParticipantesTipo AS TABLE(
	Folio BIGINT,
	CIF NVARCHAR(100),
	Nombre NVARCHAR(100),
	SegundoNombre NVARCHAR(100),
	PrimerApellido NVARCHAR(100),
	SegundoApellido NVARCHAR(100),
	Telefono NVARCHAR(20),
	Domicilio NVARCHAR(100),
	Estado NVARCHAR(50),
	Plaza INT,
	IdZona INT
);