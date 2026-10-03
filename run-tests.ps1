$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__Pedidos)) {
    Write-Error "Configure ConnectionStrings__Pedidos para SQL Server antes de ejecutar las pruebas de integración."
    exit 1
}

function Invoke-DotnetStep {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [scriptblock] $Command
    )

    Write-Host "`n=== $Name ==="
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name terminó con código $LASTEXITCODE."
    }
}

try {
    $resultsRoot = Join-Path $PSScriptRoot "TestResults"
    Invoke-DotnetStep "Restaurar dependencias" { dotnet restore PedidoSolution.slnx }
    Invoke-DotnetStep "Compilar solución Release" { dotnet build PedidoSolution.slnx --no-restore -c Release }
    Invoke-DotnetStep "Pruebas unitarias y cobertura" {
        dotnet test tests/Pedidos.UnitTests/Pedidos.UnitTests.csproj --no-build -c Release --collect:"XPlat Code Coverage" --results-directory (Join-Path $resultsRoot "Unit")
    }
    Invoke-DotnetStep "Pruebas de integración y cobertura" {
        dotnet test tests/Pedidos.IntegrationTests/Pedidos.IntegrationTests.csproj --no-build -c Release --collect:"XPlat Code Coverage" --results-directory (Join-Path $resultsRoot "Integration")
    }

    $unitReport = Get-ChildItem (Join-Path $resultsRoot "Unit") -Filter "coverage.cobertura.xml" -Recurse |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $integrationReport = Get-ChildItem (Join-Path $resultsRoot "Integration") -Filter "coverage.cobertura.xml" -Recurse |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $unitReport -or -not $integrationReport) {
        throw "Falta un archivo coverage.cobertura.xml de las pruebas."
    }

    Write-Host "`nReportes Cobertura:"
    Write-Host $unitReport.FullName
    Write-Host $integrationReport.FullName
    if (Get-Command reportgenerator -ErrorAction SilentlyContinue) {
        $htmlDirectory = Join-Path $resultsRoot "CodeCoverage"
        Invoke-DotnetStep "Generar cobertura HTML del código propio" {
            $reportArgs = @(
                "-reports:$($unitReport.FullName);$($integrationReport.FullName)",
                "-targetdir:$htmlDirectory",
                "-reporttypes:Html;TextSummary",
                "-classfilters:-Microsoft.AspNetCore.OpenApi.Generated*;-System.Runtime.CompilerServices*"
            )
            reportgenerator @reportArgs
        }
        Write-Host "Reporte HTML: $(Join-Path $htmlDirectory 'index.html')"
    }
    Write-Host "Resultados: $resultsRoot"
    exit 0
}
catch {
    Write-Error $_
    exit 1
}
