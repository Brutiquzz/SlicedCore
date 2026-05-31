<#
.SYNOPSIS
	Runs the template smoke tests inside an isolated Docker container.

.DESCRIPTION
	Builds a Docker image containing the full repository and executes
	tests/Test-Templates.ps1 inside it.  All 'dotnet new install' calls
	happen inside the container — your local dotnet template registry is
	never modified.

	Run from the repository root:

		pwsh ./tests/Run-InDocker.ps1

.PARAMETER ImageTag
	Tag to use for the locally built image. Defaults to 'template-tests:local'.

.PARAMETER NoBuild
	Skip rebuilding the image if it already exists (useful for iterating on
	the test script without changing the Dockerfile).
#>

[CmdletBinding()]
param(
	[string]$ImageTag = "template-tests:local",
	[switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

# ── Verify Docker is available ─────────────────────────────────────────────
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
	Write-Error "Docker is not installed or not on PATH. Install Docker Desktop and try again."
}

# ── Build image ────────────────────────────────────────────────────────────
if (-not $NoBuild) {
	Write-Host ""
	Write-Host "Building Docker image '$ImageTag'..." -ForegroundColor Cyan
	docker build -f (Join-Path $PSScriptRoot "Dockerfile") -t $ImageTag $RepoRoot
	if ($LASTEXITCODE -ne 0) {
		Write-Error "docker build failed (exit code $LASTEXITCODE)."
	}
	Write-Host "Image built successfully." -ForegroundColor Green
}

# ── Run tests inside container ─────────────────────────────────────────────
Write-Host ""
Write-Host "Running template smoke tests in container '$ImageTag'..." -ForegroundColor Cyan
docker run --rm $ImageTag
$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
	Write-Host "══════════════════════════════════════════════" -ForegroundColor Green
	Write-Host "  All template tests passed (container run)." -ForegroundColor Green
	Write-Host "══════════════════════════════════════════════" -ForegroundColor Green
} else {
	Write-Host "══════════════════════════════════════════════" -ForegroundColor Red
	Write-Host "  Template tests FAILED (exit code $exitCode)." -ForegroundColor Red
	Write-Host "══════════════════════════════════════════════" -ForegroundColor Red
	exit $exitCode
}
