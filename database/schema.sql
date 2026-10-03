IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Tipo TINYINT NOT NULL CHECK (Tipo IN (0, 1, 2))
    );
END;

IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Productos (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Nombre NVARCHAR(120) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Precios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Precios (
        ProductoId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Valor DECIMAL(28, 10) NOT NULL,
        CONSTRAINT FK_Precios_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(Id)
    );
END;

IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ClienteId UNIQUEIDENTIFIER NOT NULL,
        Subtotal DECIMAL(28, 10) NOT NULL,
        Descuento DECIMAL(28, 10) NOT NULL,
        MontoConDescuento DECIMAL(28, 10) NOT NULL,
        Impuesto DECIMAL(28, 10) NOT NULL,
        Total DECIMAL(28, 10) NOT NULL,
        CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id)
    );
END;

IF OBJECT_ID(N'dbo.DetallesPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetallesPedido (
        PedidoId UNIQUEIDENTIFIER NOT NULL,
        ProductoId UNIQUEIDENTIFIER NOT NULL,
        Cantidad INT NOT NULL CHECK (Cantidad >= 0),
        CONSTRAINT PK_DetallesPedido PRIMARY KEY (PedidoId, ProductoId),
        CONSTRAINT FK_DetallesPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos(Id),
        CONSTRAINT FK_DetallesPedido_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(Id)
    );
END;
