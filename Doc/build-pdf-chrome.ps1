# Doc/build-pdf-chrome.ps1
# Converts one Markdown file to PDF via PowerShell 7's ConvertFrom-Markdown
# + Chrome headless print-to-pdf. Zero-npm, zero-pandoc.
#
# Usage:  pwsh Doc/build-pdf-chrome.ps1 -Input Doc/OPTIMIZATION_SUMMARY.md
#         (optional) -Output Doc/pdf/OPTIMIZATION_SUMMARY.pdf

param(
    [Parameter(Mandatory=$true)][string]$InputFile,
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'
$InputFilePath = (Resolve-Path $InputFile).Path
if (-not $OutputFile)
{
    $baseName = [IO.Path]::GetFileNameWithoutExtension($InputFilePath)
    $docDir   = Split-Path $InputFilePath -Parent
    $OutputFile   = Join-Path $docDir "pdf/$baseName.pdf"
}
$outDir = Split-Path $OutputFile -Parent
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

# Locate Chrome
$chrome = $null
foreach ($p in @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "$env:ProgramFiles(x86)\Google\Chrome\Application\chrome.exe",
    "$env:ProgramFiles(x86)\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
))
{
    if (Test-Path $p) { $chrome = $p; break }
}
if (-not $chrome) { throw 'Chrome or Edge not found — install one.' }

# Convert MD -> HTML fragment
$html = (ConvertFrom-Markdown -Path $InputFilePath).Html

# Wrap in a print-friendly HTML template
$template = @"
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>$([IO.Path]::GetFileNameWithoutExtension($InputFilePath))</title>
<style>
  @page { size: A4; margin: 18mm 15mm 20mm 15mm; }
  html, body { font-family: -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; font-size: 10.5pt; color: #1a1a1a; line-height: 1.45; }
  h1 { font-size: 22pt; margin: 0 0 4pt 0; border-bottom: 2px solid #333; padding-bottom: 4pt; page-break-after: avoid; }
  h2 { font-size: 15pt; margin: 18pt 0 4pt 0; border-bottom: 1px solid #bbb; padding-bottom: 2pt; page-break-after: avoid; }
  h3 { font-size: 12pt; margin: 12pt 0 3pt 0; color: #333; page-break-after: avoid; }
  h4 { font-size: 11pt; margin: 8pt 0 2pt 0; color: #444; page-break-after: avoid; }
  p  { margin: 5pt 0; }
  ul, ol { margin: 5pt 0 5pt 22pt; padding: 0; }
  li { margin: 2pt 0; }
  table { border-collapse: collapse; width: 100%; margin: 6pt 0 10pt 0; font-size: 9.5pt; page-break-inside: avoid; }
  th, td { border: 1px solid #ccc; padding: 4pt 6pt; text-align: left; vertical-align: top; }
  th { background: #f0f0f0; font-weight: 600; }
  tr:nth-child(even) td { background: #fafafa; }
  code { font-family: Consolas, "Courier New", monospace; font-size: 9pt; background: #f4f4f4; padding: 1pt 3pt; border-radius: 2pt; }
  pre { background: #f4f4f4; padding: 8pt; border-radius: 3pt; font-size: 8.5pt; white-space: pre-wrap; word-break: break-word; overflow-wrap: anywhere; }
  hr { border: none; border-top: 1px solid #ccc; margin: 12pt 0; }
  blockquote { border-left: 3pt solid #666; padding: 2pt 8pt; margin: 6pt 0; background: #f8f8f8; color: #444; }
  strong { color: #000; }
  em { color: #444; }
  /* Right-align number-heavy columns */
  td:nth-last-child(-n+3) { text-align: right; }
  /* Keep headings + following block together */
  h1 + *, h2 + *, h3 + * { page-break-before: avoid; }
</style>
</head>
<body>
$html
</body>
</html>
"@

# Write HTML to temp
$tmpHtml = [IO.Path]::Combine([IO.Path]::GetTempPath(), [IO.Path]::GetRandomFileName() + '.html')
Set-Content -Path $tmpHtml -Value $template -Encoding UTF8

# Chrome headless -> PDF (absolute file path required as file:// URL)
$fileUri = 'file:///' + ($tmpHtml -replace '\\', '/')
$absOutput = [IO.Path]::GetFullPath($OutputFile)

$chromeArgs = @(
    '--headless=new'
    '--disable-gpu'
    '--no-pdf-header-footer'
    "--print-to-pdf=$absOutput"
    '--no-margins'
    $fileUri
)

Write-Host "  input : $InputFilePath"
Write-Host "  html  : $tmpHtml"
Write-Host "  chrome: $chrome"
Write-Host "  output: $absOutput"

& $chrome @chromeArgs 2>$null | Out-Null
Start-Sleep -Milliseconds 400  # let Chrome flush

Remove-Item $tmpHtml -Force -ErrorAction SilentlyContinue

if (Test-Path $absOutput)
{
    $size = (Get-Item $absOutput).Length
    Write-Host "  OK — $absOutput  ($([Math]::Round($size/1KB, 1)) KB)"
    exit 0
}
else
{
    throw "PDF was not produced at $absOutput"
}
