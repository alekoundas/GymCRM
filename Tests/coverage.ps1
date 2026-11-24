# Runs every test, measures which lines they reach, and writes coveragereport/index.html.

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot

try {
    foreach ($folder in 'TestResults', 'coveragereport') {
        if (Test-Path $folder) { Remove-Item $folder -Recurse -Force -Confirm:$false }
    }

    dotnet tool restore

    dotnet test --solution Tests.slnx --results-directory TestResults -- `
        --coverlet --coverlet-output-format cobertura `
        --coverlet-include '[Core]*' --coverlet-include '[DataAccess]*' --coverlet-include '[Business]*' --coverlet-include '[API]*' `
        --coverlet-exclude '[DataAccess]DataAccess.Migrations.*'

    dotnet tool run reportgenerator '-reports:TestResults/**/*.xml' '-targetdir:coveragereport' '-reporttypes:Html;TextSummary'

    Get-Content coveragereport/Summary.txt | Select-Object -First 20
}
finally {
    Pop-Location
}
