param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "开始发布 FloatingShortcut..."

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $repoRoot "Source"))) {
    $repoRoot = $PSScriptRoot
}

$projectPath = Join-Path $repoRoot "Source\FloatingShortcut\FloatingShortcut.csproj"
$publishDir = Join-Path $repoRoot ".publish-root"
$targetExe = Join-Path $repoRoot "FloatingShortcut.exe"

if (-not (Test-Path $projectPath)) {
    throw "找不到项目文件：$projectPath"
}

Write-Host "清理旧文件..."
if (Test-Path $targetExe) {
    Remove-Item $targetExe -Force
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    -o $publishDir

$publishedExe = Join-Path $publishDir "FloatingShortcut.exe"
if (-not (Test-Path $publishedExe)) {
    throw "发布未生成目标文件：$publishedExe"
}

Copy-Item $publishedExe $targetExe -Force
Remove-Item $publishDir -Recurse -Force

Write-Host "发布完成：$targetExe"

$projectRoot = Join-Path $repoRoot "Source\FloatingShortcut"
$binDir = Join-Path $projectRoot "bin"
$objDir = Join-Path $projectRoot "obj"

if (Test-Path $binDir) {
    Remove-Item $binDir -Recurse -Force
}

if (Test-Path $objDir) {
    Remove-Item $objDir -Recurse -Force
}

Write-Host "已清理 bin/obj"
