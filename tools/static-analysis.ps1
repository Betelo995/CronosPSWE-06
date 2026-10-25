# Runs the Roslyn analyzers over src/Cronos (net6.0) and writes the build output,
# followed by a warning count grouped by rule code, to the given path.
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Continue'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src/Cronos/Cronos.csproj'

$outputDir = Split-Path -Parent $OutputPath
if ($outputDir -and -not (Test-Path $outputDir)) { New-Item -ItemType Directory -Force $outputDir | Out-Null }

$lines = & cmd /c "dotnet build `"$project`" -f net6.0 /p:EnableNETAnalyzers=true /p:AnalysisMode=All /p:TreatWarningsAsErrors=false -v normal 2>&1"
$exitCode = $LASTEXITCODE

$counts = @{}
foreach ($line in $lines) {
    foreach ($m in [regex]::Matches($line, 'warning ((CA|IDE|CS)\d+)')) {
        $rule = $m.Groups[1].Value
        if ($counts.ContainsKey($rule)) { $counts[$rule]++ } else { $counts[$rule] = 1 }
    }
}
$unruled = @($lines | Where-Object { $_ -match ': warning :' }).Count

$summary = @('', '--- WARNING COUNT BY RULE (occurrences in build output) ---')
foreach ($prefix in 'CA', 'IDE', 'CS') {
    $rules = @($counts.Keys | Where-Object { $_ -match "^$prefix\d+$" } | Sort-Object)
    $total = 0
    foreach ($r in $rules) { $total += $counts[$r] }
    $summary += "$($prefix)xxxx: $total"
    foreach ($r in $rules) { $summary += "  $($r): $($counts[$r])" }
}
$summary += "Warnings without rule code (occurrences): $unruled"
$summary += "dotnet build exit code: $exitCode"

($lines + $summary) | Out-File -FilePath $OutputPath -Encoding utf8
$summary | ForEach-Object { Write-Output $_ }
exit $exitCode
