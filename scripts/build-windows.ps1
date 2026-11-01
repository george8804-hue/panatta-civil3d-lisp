param([string]$OutputDirectory = "artifacts/windows-0.1.0")
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    if (Test-Path $OutputDirectory) { throw "Diretorio ja existe; escolha outro -OutputDirectory." }
    & dotnet run --project tests/Panatta.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw "Testes falharam." }
    & dotnet publish src/Panatta.Cli -c Release -r win-x64 --self-contained true -o "$OutputDirectory/engine" -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw "Publicacao falhou." }
    Copy-Item cad/PANATTAMEMORIAL.lsp $OutputDirectory
    Copy-Item README.md $OutputDirectory
    Copy-Item docs "$OutputDirectory/docs" -Recurse
    Copy-Item examples "$OutputDirectory/examples" -Recurse
    $root = (Resolve-Path $OutputDirectory).Path
    Get-ChildItem $root -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
        "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $relative
    } | Set-Content "$root/SHA256SUMS.txt" -Encoding UTF8
    Compress-Archive -Path "$OutputDirectory/*" -DestinationPath "$OutputDirectory.zip"
    Write-Output "Pacote gerado: $OutputDirectory.zip. Ainda exige homologacao no Civil 3D."
} finally { Pop-Location }
