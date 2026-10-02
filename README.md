# Módulo de Pedidos

## Contexto del caso

Una empresa de comercio electrónico necesita procesar pedidos, calcular descuentos e impuestos, conservar pedidos durante la vida de la aplicación y consultarlos por identificador.

## Tecnologías

- C# y .NET 10 (`net10.0` en los tres proyectos).
- ASP.NET Core Minimal API.
- xUnit 2.9.3.
- Moq 4.21.0 para dobles de persistencia en pruebas unitarias.
- `Microsoft.AspNetCore.Mvc.Testing` 10.0.12 para pruebas HTTP de integración.
- `coverlet.collector` para reportes Cobertura.

## Justificación del lenguaje y framework

Se utiliza C#/.NET, el lenguaje y plataforma elegidos para el proyecto, y xUnit como framework de pruebas para .NET.

## Estructura del proyecto

```text
Semana 7/
├── PedidoSolution.slnx
├── src/
│   └── Pedidos.Api/
│       ├── Domain/
│       │   ├── Cliente.cs
│       │   ├── Pedido.cs
│       │   └── Producto.cs
│       ├── Repositories/
│       │   ├── IPedidoRepository.cs
│       │   └── PedidoRepository.cs
│       ├── Services/
│       │   └── PedidoService.cs
│       ├── Pedidos.Api.csproj
│       └── Program.cs
├── tests/
│   ├── Pedidos.UnitTests/
│   │   ├── Pedidos.UnitTests.csproj
│   │   └── PedidoServiceTests.cs
│   └── Pedidos.IntegrationTests/
│       ├── Pedidos.IntegrationTests.csproj
│       └── PedidosEndpointTests.cs
├── run-tests.ps1
└── README.md
```

## Reglas de negocio

- Cliente Regular: 0 % de descuento.
- Cliente VIP: 10 % del subtotal.
- Cliente Mayorista: 20 % cuando el subtotal es mayor que 500; 5 % cuando es menor o igual a 500.
- Impuesto: 18 % aplicado al monto después del descuento.
- Total: monto con descuento más impuesto.
- Subtotal: suma de precio por cantidad para cada producto.

## Instalación

Requiere el SDK de .NET 10. Desde la raíz del repositorio:

```powershell
dotnet restore PedidoSolution.slnx
```

## Ejecución

```powershell
dotnet build PedidoSolution.slnx
dotnet run --project src/Pedidos.Api
```

La API queda disponible en las direcciones locales que muestre ASP.NET Core al iniciar.

## Ejecución de pruebas

```powershell
dotnet test PedidoSolution.slnx
```

Resultado validado: 15 pruebas aprobadas (11 unitarias y 4 de integración), sin errores.

## Automatización

```powershell
.\run-tests.ps1
```

El script trabaja desde su propia ubicación, restaura paquetes, compila Release, ejecuta por separado las pruebas unitarias y de integración, recopila cobertura y devuelve un código distinto de cero si falla una etapa. Si PowerShell bloquea la ejecución, puede ejecutarse puntualmente con:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-tests.ps1
```

Esto no cambia la política global del equipo.

## API

El JSON usa nombres camelCase. `tipo` es el enum `TipoCliente`: Regular = 0, Vip = 1, Mayorista = 2. El POST solo usa `cliente` y `productos` como datos de entrada; los valores calculados y el ID se generan en el servicio.

### `POST /pedidos`

Request:

```json
{
  "cliente": { "tipo": 1 },
  "productos": [
    { "precio": 100.00, "cantidad": 2 }
  ]
}
```

Respuesta `201 Created` (extracto):

```json
{
  "id": "5f8a21d3-a626-4848-96d8-70b78f7923ef",
  "cliente": { "tipo": 1 },
  "productos": [{ "precio": 100.00, "cantidad": 2 }],
  "subtotal": 200.00,
  "descuento": 20.00,
  "montoConDescuento": 180.00,
  "impuesto": 32.40,
  "total": 212.40
}
```

El identificador del ejemplo es ilustrativo; cada pedido recibe un `Guid` nuevo. Una cantidad negativa devuelve `400 Bad Request`.

### `GET /pedidos/{id}`

Devuelve `200 OK` y el pedido cuando el `Guid` existe; un identificador desconocido devuelve `404 Not Found`.

## Casos de pruebas unitarias

Todas las pruebas unitarias siguen Arrange, Act, Assert, con los tres comentarios en el código. Moq sustituye el repositorio únicamente en los casos unitarios de crear y recuperar.

| Prueba | Propósito | Resultado esperado |
|---|---|---|
| `test_calcular_subtotal_pedido_vacio` | Pedido sin productos | Subtotal 0 |
| `test_calcular_subtotal_con_productos` | Suma precio por cantidad en varios productos | Subtotal 37 |
| `test_cantidad_negativa_debe_ser_rechazada` | Validar cantidad inválida | `ArgumentOutOfRangeException` |
| `test_descuento_cliente_regular` | Cliente Regular | Descuento 0 |
| `test_descuento_cliente_vip` | Cliente VIP | Descuento del 10 % |
| `test_descuento_mayorista_segun_monto` | Montos 600, 500 y 400 | Descuentos 120, 25 y 20 |
| `test_calcular_impuesto_y_total` | Impuesto y total luego de descuento | Impuesto 16.20 y total 106.20 sobre 90 |
| `test_crear_pedido_persiste_el_pedido_con_identificador` | Crear con repositorio doble | ID no vacío y guardado una vez |
| `test_recuperar_pedido_por_identificador` | Recuperar con repositorio doble | Se devuelve el pedido configurado |

La teoría mayorista cuenta como tres casos ejecutados; por eso el total de pruebas unitarias es 11.

## Casos de pruebas de integración

Las pruebas usan `WebApplicationFactory<Program>` y ejercitan HTTP → endpoint → servicio → repositorio real en memoria. Cada fábrica tiene su propia aplicación y estado.

| Prueba | Propósito | Resultado esperado |
|---|---|---|
| `Post_pedidos_devuelve_pedido_calculado` | Crear pedido VIP por HTTP | 201, ID y cálculos correctos |
| `Get_pedidos_devuelve_pedido_previamente_creado` | Crear y luego recuperar | 200 y datos coincidentes |
| `Get_pedidos_inexistente_devuelve_404` | Consultar un Guid aleatorio | 404 |
| `Post_pedidos_con_cantidad_negativa_devuelve_400` | Enviar cantidad negativa | 400 |

## Cobertura

La automatización ejecuta `dotnet test` con `--collect:"XPlat Code Coverage"`. Los archivos `coverage.cobertura.xml` quedan en `TestResults/Unit/` y `TestResults/Integration/`; las carpetas de resultados tienen identificadores de ejecución.

En la última ejecución del script, al combinar por número de línea los reportes unitario y de integración del código propio de `Pedidos.Api`, se cubrieron 77 de 82 líneas únicas (93.90 %). El cálculo excluye clases generadas de OpenAPI y combina la cobertura de ambos reportes para no contar dos veces las líneas compartidas. Los XML originales también contienen código generado por OpenAPI, por lo que sus porcentajes globales individuales son distintos.

## Evidencia TDD

El flujo de los cinco ciclos se conserva en commits sucesivos, sin squash ni reescritura. Para inspeccionarlo:

```powershell
git log --oneline
```

| Ciclo | RED | GREEN | REFACTOR |
|---|---|---|---|
| 1. Subtotal vacío | `af34fda` | `17424a1` | `72ec73b` |
| 2. Productos y validación de cantidad | `e754bf4` (corrección del test en `3dbb9fc`) | `2f56a32` | `8b7ac6c` |
| 3. Descuento VIP | `ffdfab6` | `ca06b22` | `96d4e06` |
| 4. Descuento mayorista y límite 500 | `e918f8c` | `a13a7de` | `68072c6` |
| 5. Impuesto y total | `9e76004` | `d8bba07` | `8bdd500` |

Nota de trazabilidad: la refactorización del ciclo 1 generalizó el subtotal a varios productos antes de escribir la prueba dedicada del ciclo 2. Por ello, esa parte de la prueba de productos ya pasaba al iniciar el ciclo 2; el RED verificable del segundo ciclo fue el caso de cantidad negativa, que falló por no lanzar la excepción. También hubo que corregir la forma de la aserción en `3dbb9fc`; `e754bf4` conserva la prueba corregida antes de la implementación. El historial no se reescribió.

## Decisiones de diseño

- `decimal` para subtotal, descuento, impuesto y total.
- `Guid` para identificar pedidos.
- `PedidoRepository` usa memoria concurrente y conserva datos durante la vida de esa instancia de la aplicación. No persiste entre procesos ni requiere simular supervivencia entre instancias.
- **Decisión adoptada para resolver el caso límite solicitado:** las cantidades negativas se rechazan con `ArgumentOutOfRangeException`. El enunciado no fijaba este resultado. Una cantidad 0 se permite y aporta 0 al subtotal.
- Mayorista con subtotal exactamente 500 recibe el descuento del 5 % porque la tasa del 20 % requiere `subtotal > 500`.
- La entrada del POST reutiliza el modelo de dominio `Pedido`; el servicio ignora los valores calculados que lleguen en la solicitud y los vuelve a calcular.

## Reflexión

### Borrador de reflexión para revisión del estudiante

1. En el primer ciclo observé cómo una prueba pendiente hace visible una capacidad que todavía no existe.
2. Implementar solo lo necesario permitió comenzar con una operación pequeña y concreta.
3. La refactorización dejó el cálculo de subtotal más directo.
4. En el trabajo, el cálculo general de varios productos se adelantó a la prueba dedicada de ese caso, como se explica en la trazabilidad TDD.
5. El caso mayorista de 500 mostró por qué conviene probar exactamente los límites de una condición.
6. Las pruebas de descuentos y totales ayudaron a mantener la regla del impuesto después del descuento.
7. Moq permitió comprobar que el servicio guardaba y consultaba pedidos sin depender del repositorio en esas pruebas unitarias.
8. Las pruebas de integración ejercitaron solicitudes HTTP con el servicio y el repositorio en memoria reales.
9. La fábrica de pruebas aisló cada aplicación y evitó depender del orden o de ejecuciones anteriores.
10. El script reunió restauración, compilación, pruebas unitarias, integración y cobertura en una misma ejecución repetible.
11. xUnit permitió expresar casos simples con `Fact` y varias fronteras de descuento con `Theory` e `InlineData`.
12. La salida de cobertura también me llevó a distinguir el código propio del código generado por OpenAPI al interpretar el porcentaje.
