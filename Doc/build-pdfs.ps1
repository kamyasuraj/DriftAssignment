# Doc/build-pdfs.ps1
# Regenerates PDF copies of every Markdown doc into Doc/pdf/.
# Tries Pandoc first (best fidelity), falls back to md-to-pdf (npm).
#
# Usage:  pwsh Doc/build-pdfs.ps1
# Reruns are safe — output PDFs are overwritten.

$ErrorActionPreference = 'Stop'

$DocDir  = $PSScriptRoot
$OutDir  = Join-Path $DocDir 'pdf'
if (-not (Test-Path $OutDir))
{
    New-Item -ItemType Directory -Path $OutDir | Out-Null
}

$MdFiles = Get-ChildItem -Path $DocDir -Filter '*.md' -File
if ($MdFiles.Count -eq 0)
{
    Write-Host 'No Markdown files found in Doc/. Nothing to build.'
    exit 0
}

function Try-Pandoc
{
    $cmd = Get-Command pandoc -ErrorAction SilentlyContinue
    if ($null -eq $cmd) { return $false }

    Write-Host "Using Pandoc ($($cmd.Source))"
    foreach ($md in $MdFiles)
    {
        $pdf = Join-Path $OutDir ($md.BaseName + '.pdf')
        Write-Host "  -> $($md.Name)  =>  $($pdf)"
        # weasyprint or wkhtmltopdf back-end; pandoc picks whatever is available
        & pandoc $md.FullName -o $pdf --from=gfm --pdf-engine=weasyprint 2>$null
        if ($LASTEXITCODE -ne 0)
        {
            # try wkhtmltopdf fallback
            & pandoc $md.FullName -o $pdf --from=gfm --pdf-engine=wkhtmltopdf 2>$null
        }
        if ($LASTEXITCODE -ne 0)
        {
            Write-Warning "  Pandoc failed for $($md.Name) — will try md-to-pdf fallback."
            return $false
        }
    }
    return $true
}

function Try-MdToPdf
{
    $cmd = Get-Command npx -ErrorAction SilentlyContinue
    if ($null -eq $cmd)
    {
        Write-Warning 'npx not found. Install Node.js to use md-to-pdf fallback.'
        return $false
    }

    Write-Host 'Using md-to-pdf via npx'
    foreach ($md in $MdFiles)
    {
        $pdf = Join-Path $OutDir ($md.BaseName + '.pdf')
        Write-Host "  -> $($md.Name)  =>  $($pdf)"
        & npx --yes md-to-pdf $md.FullName --dest $OutDir
        if ($LASTEXITCODE -ne 0)
        {
            Write-Error "md-to-pdf failed for $($md.Name)"
            return $false
        }
    }
    return $true
}

if (Try-Pandoc)
{
    Write-Host "Done. PDFs written to $OutDir"
    exit 0
}

if (Try-MdToPdf)
{
    Write-Host "Done. PDFs written to $OutDir"
    exit 0
}

Write-Error @'
Neither Pandoc nor md-to-pdf could be run.
Install one of:
  - Pandoc:      https://pandoc.org/installing.html  (+ weasyprint or wkhtmltopdf)
  - Node.js:     https://nodejs.org  (then npx md-to-pdf will work)
'@
exit 1
