# Módulo de Pedidos

## Descripción del módulo

Esta aplicación permite crear y consultar pedidos desde una página web sencilla y desde la API. Al crear un pedido calcula el subtotal de los productos, aplica el descuento del cliente y suma el impuesto del 18 % sobre el monto descontado.

Los pedidos se guardan en SQL Server. El esquema mínimo está en `database/schema.sql` y contiene `Clientes`, `Productos`, `Precios`, `Pedidos` y `DetallesPedido`. Esta última tabla relaciona cada pedido con sus productos y cantidades. La aplicación crea la base de datos y las tablas que falten al iniciar; los datos permanecen después de cerrar la API.

La API ofrece `POST /pedidos` para crear y `GET /pedidos/{id}` para consultar. La interfaz web se abre en la dirección local que muestre `dotnet run`, por ejemplo `http://localhost:5127/`.

## Instalación y ejecución

Se necesita .NET SDK 10 y SQL Server 2022 o compatible. Para levantar un contenedor nuevo en otro equipo, abre PowerShell en la raíz del repositorio y define una contraseña propia que cumpla los requisitos de SQL Server:

```powershell
$env:MSSQL_SA_PASSWORD = "<contraseña_segura>"
docker compose -f database/compose.yaml up -d
```

Si ya tienes SQL Server disponible en el puerto 1433, omite ese paso. Configura la conexión en la misma sesión de PowerShell, con la contraseña de tu servidor:

```powershell
$env:ConnectionStrings__Pedidos = "Server=127.0.0.1,1433;Database=PedidosDb;User Id=sa;Password=$env:MSSQL_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
dotnet restore PedidoSolution.slnx
dotnet build PedidoSolution.slnx
dotnet run --project src/Pedidos.Api
```

También puedes definir `ConnectionStrings__Pedidos` con otro servidor, usuario y base de datos. El usuario SQL debe poder crear la base de datos la primera vez. La contraseña se configura localmente y no se incluye en Git. El script `database/schema.sql` queda disponible para revisión o ejecución manual en una base ya creada.

Para ejecutar todas las pruebas, mantén SQL Server activo y la variable `ConnectionStrings__Pedidos` configurada:

```powershell
dotnet test PedidoSolution.slnx
```

Para restaurar, compilar en Release, ejecutar pruebas y recopilar cobertura en un solo paso:

```powershell
.\run-tests.ps1
```

Si PowerShell bloquea el script, puedes ejecutarlo puntualmente sin cambiar la política global:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-tests.ps1
```

## Casos de prueba

### Pruebas unitarias

Las pruebas de `PedidoServiceTests.cs` aíslan los cálculos. En creación y consulta usan Moq para comprobar la colaboración con el repositorio sin usar SQL Server.

| Caso | Propósito y resultado esperado |
|---|---|
| Pedido vacío | El subtotal es 0. |
| Varios productos | `12,50 × 2 + 4 × 3 = 37`. |
| Cantidad cero | No cambia el subtotal. |
| Cantidad negativa | Se rechaza con `ArgumentOutOfRangeException`. |
| Cliente Regular | Descuento 0 %. |
| Cliente VIP | Descuento 10 %. |
| Cliente Mayorista | Sobre 500 aplica 20 %; en 500 o menos aplica 5 %. |
| Impuesto y total | Se aplica 18 % después del descuento. |
| Subtotal o monto descontado en cero | Regular y VIP no reciben descuento; el impuesto y el total son 0. |
| Crear y recuperar | Se genera un ID y se llama al repositorio según corresponda. |

Se ejecutaron **15 casos unitarios**, contando las entradas de las pruebas parametrizadas para clientes Regular, VIP y Mayorista.

### Pruebas de integración

`PedidosEndpointTests.cs` inicia la API real y usa una base SQL temporal distinta por prueba. Cada base se elimina al terminar, sin depender del orden de ejecución.

| Caso | Propósito y resultado esperado |
|---|---|
| `POST /pedidos` | Devuelve 201, cálculos correctos y filas en las cinco tablas SQL. |
| `GET /pedidos/{id}` | Devuelve 200 con el pedido guardado y sus productos. |
| ID inexistente | Devuelve 404. |
| Cantidad negativa | Devuelve 400 sin insertar el pedido. |
| Nueva instancia de la API | Recupera desde SQL un pedido creado por una instancia anterior. |

Se ejecutaron **5 pruebas de integración**. Los criterios de aceptación y el código de las pruebas están documentados en `docs/Criterios_de_aceptacion_pedidos.docx`.

## Métricas de cobertura obtenidas

`run-tests.ps1` guarda los XML Cobertura en `TestResults/Unit/` y `TestResults/Integration/`. Si ReportGenerator está instalado, también genera `TestResults/CodeCoverage/index.html` y `Summary.txt`.

En la ejecución del 3 de octubre de 2026, la cobertura combinada del código propio de `Pedidos.Api` fue **97 % de líneas (195 de 201)** y **84,3 % de ramas (27 de 32)**. Para calcularla se combinaron los reportes unitario y de integración y se excluyó el código generado por OpenAPI y el compilador. El porcentaje global sin ese filtro es diferente.

## Reflexión

El TDD nos mostró que las pruebas pueden acompañar la construcción del proyecto desde el comienzo, y no solamente revisarlo al final.

En la fase roja escribimos una prueba para una característica concreta y comprobamos que fallara por la razón esperada.

En la fase verde agregamos el código mínimo para cumplir ese comportamiento y volvimos a ejecutar las pruebas.

Después, en la refactorización, pudimos ordenar el código sin cambiar el resultado que las pruebas ya protegían.

Una de las dificultades fue «pensar al revés»: definir primero qué debía ocurrir cuando todavía no existía la implementación.

Esto nos resultó contraintuitivo porque teníamos el hábito de programar primero y probar después.

También aprendimos que una prueba necesita un criterio de aceptación claro; de lo contrario, un número esperado puede parecer arbitrario.

El límite de 500 para el descuento mayorista nos ayudó a entender por qué conviene probar exactamente el valor donde cambia la regla.

Con xUnit organizamos los casos mediante `[Fact]` y usamos `[Theory]` con varios datos para comprobar una misma regla en diferentes situaciones.

Sus `Assert` nos permitieron expresar de forma directa qué resultado esperábamos y detectar si un cambio rompía el comportamiento.

Moq nos ayudó a aislar el servicio en las pruebas unitarias, mientras que las pruebas de integración comprobaron la API junto con SQL Server.

Por último, el script de automatización hizo más fácil repetir las pruebas y revisar la cobertura después de cada cambio.
