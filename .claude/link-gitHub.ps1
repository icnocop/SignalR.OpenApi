#Requires -Version 5.1
<#
.SYNOPSIS
    Links the repository's GitHub Copilot skills folder into the Claude Code skills location
    so a single set of skill definitions serves both tools.

.DESCRIPTION
    Creates '.claude/skills' as a link to the Copilot skills folder (by default
    '.github/copilot/skills'). The SKILL.md frontmatter Copilot uses is already the format
    Claude Code expects, so no content changes are needed.

    The script is idempotent: if the link already points at the right place it reports that
    and exits without changing anything. It will not delete a real directory of files.

.PARAMETER Symbolic
    Create a symbolic link instead of a directory junction. Junctions are the default
    because they work on Windows without elevation or Developer Mode. Use -Symbolic for
    cross-volume targets or on non-Windows platforms.

.PARAMETER Force
    Replace an existing link that points somewhere else. Has no effect on a real directory
    containing files; remove that yourself if you really mean to.

.EXAMPLE
    pwsh -File .claude/link-gitHub.ps1

.EXAMPLE
    pwsh -File .claude/link-gitHub.ps1 -Symbolic -Force
#>
[CmdletBinding()]
param(
    [switch]$Symbolic,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$target = Join-Path $PSScriptRoot 'skills'

# Probe the conventional locations, most specific first, so the script keeps working if
# the skills folder is ever moved to '.github/skills'.
$candidates = @(
    (Join-Path $repoRoot '.github/skills'),
    (Join-Path $repoRoot '.github/copilot/skills')
)

$source = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Container } | Select-Object -First 1

if (-not $source) {
    Write-Error ("No skills folder found. Looked for:`n  " + ($candidates -join "`n  "))
    exit 1
}

$source = (Resolve-Path -LiteralPath $source).Path
Write-Host "Source: $source"
Write-Host "Target: $target"

function Get-LinkTarget {
    param([string]$Path)

    $item = Get-Item -LiteralPath $Path -Force
    if (-not $item.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
        return $null
    }

    # Available on PowerShell 5.1+ for reparse points; may be empty for some link types.
    if ($item.PSObject.Properties.Name -contains 'Target' -and $item.Target) {
        return @($item.Target)[0]
    }

    return $null
}

if (Test-Path -LiteralPath $target) {
    $existingTarget = Get-LinkTarget -Path $target

    if ($null -eq $existingTarget) {
        Write-Error @"
'$target' already exists and is a real directory, not a link.
Move or delete it yourself, then re-run this script. Refusing to remove files.
"@
        exit 1
    }

    if ($existingTarget.TrimEnd('\', '/') -ieq $source.TrimEnd('\', '/')) {
        Write-Host "Already linked. Nothing to do." -ForegroundColor Green
        exit 0
    }

    if (-not $Force) {
        Write-Error @"
'$target' is already a link, but it points at:
  $existingTarget
Re-run with -Force to repoint it at:
  $source
"@
        exit 1
    }

    Write-Host "Removing existing link to '$existingTarget'."
    # Removes the link only. The reparse point's contents are not touched.
    [IO.Directory]::Delete($target, $false)
}

$itemType = if ($Symbolic) { 'SymbolicLink' } else { 'Junction' }

try {
    New-Item -ItemType $itemType -Path $target -Value $source | Out-Null
}
catch {
    if ($itemType -eq 'SymbolicLink') {
        Write-Error @"
Failed to create a symbolic link: $($_.Exception.Message)
Symbolic links require elevation or Developer Mode on Windows.
Try running without -Symbolic to create a directory junction instead.
"@
    }
    else {
        Write-Error "Failed to create junction: $($_.Exception.Message)"
    }

    exit 1
}

Write-Host "Created $itemType." -ForegroundColor Green

$skills = Get-ChildItem -Path $target -Recurse -Filter 'SKILL.md' -ErrorAction SilentlyContinue
if (-not $skills) {
    Write-Warning "Link created, but no SKILL.md files were found through it."
    exit 0
}

Write-Host "Skills now visible to Claude Code:"
foreach ($skill in $skills) {
    Write-Host "  $($skill.Directory.Name)"
}
