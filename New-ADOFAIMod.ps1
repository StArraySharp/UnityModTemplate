param(
    [Alias("n")]
    [string]$Name,

    [Alias("g")]
    [string]$GamePath,

    [Alias("a")]
    [string]$Author,

    [Alias("d")]
    [string]$Description,

    [Alias("v")]
    [string]$Version = "1.0.0",

    [Alias("o")]
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Name) -or
    [string]::IsNullOrWhiteSpace($GamePath) -or
    [string]::IsNullOrWhiteSpace($Author) -or
    [string]::IsNullOrWhiteSpace($Description)) {
    throw "Name, GamePath, Author, and Description are required."
}

if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    throw "Invalid project name '$Name'. Use ^[A-Za-z_][A-Za-z0-9_]*$; for example MyCoolMod or ADOFAI_Mod."
}

if (-not [System.IO.Path]::IsPathFullyQualified($GamePath) -or -not (Test-Path -LiteralPath $GamePath -PathType Leaf)) {
    throw "GamePath must be the full path to an existing ADOFAI .exe file."
}

$gameDirectory = [System.IO.Path]::GetDirectoryName($GamePath)
$executableBaseName = [System.IO.Path]::GetFileNameWithoutExtension($GamePath)
$managedDirectory = Join-Path (Join-Path $gameDirectory ($executableBaseName + "_Data")) "Managed"
if (-not (Test-Path -LiteralPath $managedDirectory -PathType Container)) {
    throw "The ADOFAI Managed directory was not found: $managedDirectory"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = $Name
}

& dotnet new adofaimod `
    --name $Name `
    --output $OutputDirectory `
    --game-path $GamePath `
    --author-name $Author `
    --description $Description `
    --version $Version

if ($LASTEXITCODE -ne 0) {
    throw "dotnet new failed with exit code $LASTEXITCODE. Install the template first with: dotnet new install <template directory or .nupkg>"
}
