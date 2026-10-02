# Módulo de Pedidos

## Descripción del módulo

Este módulo permite registrar y consultar pedidos de una tienda en línea. Al crear un pedido calcula el subtotal, aplica el descuento correspondiente al tipo de cliente y suma el impuesto del 18 %.

Los pedidos se guardan en memoria mientras la aplicación está en ejecución. Cada pedido recibe un identificador único. La API permite crear pedidos con `POST /pedidos` y consultarlos con `GET /pedidos/{id}`.

## Instalación y ejecución

Se requiere el SDK de .NET 10. Desde la carpeta del proyecto, restaura las dependencias:

```powershell
dotnet restore PedidoSolution.slnx
```

Para compilar y ejecutar la API:

```powershell
dotnet build PedidoSolution.slnx
dotnet run --project src/Pedidos.Api
```

Para ejecutar todas las pruebas:

```powershell
dotnet test PedidoSolution.slnx
```

También puedes ejecutar el proceso completo —restauración, compilación, pruebas y cobertura— con:

```powershell
.\run-tests.ps1
```

Los reportes de cobertura se guardan en `TestResults/`. Si PowerShell bloquea el script, puedes ejecutarlo puntualmente así, sin cambiar la política global:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-tests.ps1
```

## Casos de prueba

### Pruebas unitarias

| Caso | Propósito |
|---|---|
| Subtotal de pedido vacío | Comprobar que un pedido sin productos suma 0. |
| Subtotal con varios productos | Verificar que se suman los precios multiplicados por sus cantidades. |
| Cantidad negativa | Confirmar que el servicio rechaza una cantidad menor que cero. |
| Descuento para cliente Regular | Comprobar que no se aplica descuento. |
| Descuento para cliente VIP | Verificar el descuento del 10 %. |
| Descuento para cliente Mayorista | Revisar los montos por encima, por debajo y exactamente en 500. |
| Impuesto y total | Comprobar el impuesto del 18 % sobre el monto descontado y el total resultante. |
| Creación y persistencia | Verificar que el servicio genera un ID y guarda el pedido usando un doble de prueba. |
| Recuperación de pedido | Comprobar que el servicio devuelve el pedido obtenido del doble de prueba. |

Se ejecutaron 11 casos unitarios, incluyendo los tres montos de la prueba mayorista.

### Pruebas de integración

Estas pruebas envían solicitudes HTTP a la API y usan el servicio y el repositorio en memoria reales.

| Caso | Propósito |
|---|---|
| Crear un pedido | Comprobar la respuesta de creación, el identificador y los cálculos del pedido. |
| Consultar un pedido creado | Verificar que el pedido recuperado conserva sus datos. |
| Consultar un identificador inexistente | Confirmar que la API responde `404 Not Found`. |
| Enviar una cantidad negativa | Confirmar que la API responde `400 Bad Request`. |

Se ejecutaron 4 casos de integración.


