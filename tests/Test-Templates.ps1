<#
.SYNOPSIS
	Validates that all dotnet new templates in this repository scaffold correctly and produce
	projects that build without errors.

.DESCRIPTION
	This script is the canonical template test harness. It is called by the CI workflow
	and can also be run locally from the repository root:

		pwsh ./tests/Test-Templates.ps1

	Tests performed:
	  1. Install and scaffold the solution template (my-web)
	  2. Build the scaffolded solution
	  3. Install the domain item template and scaffold a new domain
	  4. Install the feature item template and scaffold a new feature (with and without endpoint)
	  5. Build the scaffolded solution again after item template additions
	  6. Clean up the temporary output directory

.NOTES
	Requires: .NET 10 SDK, pwsh (PowerShell 7+)
#>

[CmdletBinding()]
param(
	[string]$OutputRoot = ([System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "template-smoke-test"))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot    = Resolve-Path (Join-Path $PSScriptRoot "..")
$ProjectName = "MyTestApp"
$OutputDir   = Join-Path $OutputRoot $ProjectName

function Step([string]$message) {
	Write-Host ""
	Write-Host "──────────────────────────────────────────────" -ForegroundColor Cyan
	Write-Host "  $message" -ForegroundColor Cyan
	Write-Host "──────────────────────────────────────────────" -ForegroundColor Cyan
}

function Invoke-Checked([string]$description, [scriptblock]$block) {
	Write-Host "» $description" -ForegroundColor Yellow
	& $block
	if ($LASTEXITCODE -ne 0) {
		Write-Host "FAILED: $description (exit code $LASTEXITCODE)" -ForegroundColor Red
		exit $LASTEXITCODE
	}
	Write-Host "  OK" -ForegroundColor Green
}

# ── Cleanup previous run ────────────────────────────────────────────────────
if (Test-Path $OutputRoot) {
	Write-Host "Removing previous output at $OutputRoot"
	Remove-Item -Recurse -Force $OutputRoot
}
New-Item -ItemType Directory -Path $OutputRoot | Out-Null

try {
	# ── 1. Install solution template ─────────────────────────────────────────
	Step "1 / 5  Install solution template"
	Invoke-Checked "dotnet new install <repo-root>" {
		dotnet new install $RepoRoot --force
	}

	# ── 2. Scaffold solution ──────────────────────────────────────────────────
	Step "2 / 5  Scaffold solution template → $ProjectName"
	Invoke-Checked "dotnet new my-web -n $ProjectName" {
		dotnet new my-web -n $ProjectName -o $OutputDir --force
	}

	# ── 3. Build scaffolded solution ──────────────────────────────────────────
	Step "3 / 5  Build scaffolded solution"
	Invoke-Checked "dotnet build (initial)" {
		dotnet build $OutputDir --configuration Release
	}

	# ── 4. Scaffold domain + features ────────────────────────────────────────
	# The repo-root install (step 1) already registers the domain and feature
	# item templates — no separate install step needed.
	Step "4 / 5  Scaffold domain + features"

	# Scaffold the Order domain
	$OrderDir = Join-Path $OutputDir "src\$ProjectName\Domains\Order"
	New-Item -ItemType Directory -Path $OrderDir | Out-Null

	Invoke-Checked "dotnet new domain -n Order" {
		dotnet new domain -n Order -o $OrderDir --projectName $ProjectName
	}

	Invoke-Checked "dotnet new feature -n CreateOrder (with endpoint, POST)" {
		dotnet new feature -n CreateOrder -o $OrderDir --projectName $ProjectName --operation POST
	}

	# Scaffold a second domain to test --include-endpoint false independently
	$ProductDir = Join-Path $OutputDir "src\$ProjectName\Domains\Product"
	New-Item -ItemType Directory -Path $ProductDir | Out-Null

	Invoke-Checked "dotnet new domain -n Product" {
		dotnet new domain -n Product -o $ProductDir --projectName $ProjectName
	}

	Invoke-Checked "dotnet new feature -n CreateProduct (without endpoint, POST)" {
		dotnet new feature -n CreateProduct -o $ProductDir --projectName $ProjectName --operation POST --include-endpoint false
	}

	# Scaffold a third domain to test additional HTTP operations
	$UserDir = Join-Path $OutputDir "src\$ProjectName\Domains\User"
	New-Item -ItemType Directory -Path $UserDir | Out-Null

	Invoke-Checked "dotnet new domain -n User" {
		dotnet new domain -n User -o $UserDir --projectName $ProjectName
	}

	Invoke-Checked "dotnet new feature -n CreateUser (with endpoint, POST)" {
		dotnet new feature -n CreateUser -o $UserDir --projectName $ProjectName --operation POST
	}

	Invoke-Checked "dotnet new feature -n GetUser (GET)" {
		dotnet new feature -n GetUser -o $UserDir --projectName $ProjectName --operation GET
	}

	Invoke-Checked "dotnet new feature -n UpdateUser (PUT)" {
		dotnet new feature -n UpdateUser -o $UserDir --projectName $ProjectName --operation PUT
	}

	Invoke-Checked "dotnet new feature -n PatchUser (PATCH)" {
		dotnet new feature -n PatchUser -o $UserDir --projectName $ProjectName --operation PATCH
	}

	Invoke-Checked "dotnet new feature -n DeleteUser (DELETE)" {
		dotnet new feature -n DeleteUser -o $UserDir --projectName $ProjectName --operation DELETE
	}

	# ── 5. Build again after item template additions ───────────────────────────
	Step "5 / 5  Build scaffolded solution after item template additions"
	Invoke-Checked "dotnet build (after scaffolding domain + features)" {
		dotnet build $OutputDir --configuration Release
	}

	Write-Host ""
	Write-Host "══════════════════════════════════════════════" -ForegroundColor Green
	Write-Host "  All template tests passed." -ForegroundColor Green
	Write-Host "══════════════════════════════════════════════" -ForegroundColor Green
}
finally {
	# ── Cleanup ────────────────────────────────────────────────────────────────
	Write-Host ""
	Write-Host "Cleaning up $OutputRoot"
	if (Test-Path $OutputRoot) {
		Remove-Item -Recurse -Force $OutputRoot
	}

	# Uninstall templates to leave the environment clean
	dotnet new uninstall $RepoRoot 2>$null
}
