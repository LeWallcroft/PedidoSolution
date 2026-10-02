$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

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

    Write-Host "`nResultados y reportes de cobertura: $resultsRoot"
    Get-ChildItem -Path $resultsRoot -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object { Write-Host $_.FullName }
    exit 0
}
catch {
    Write-Error $_
    exit 1
}
