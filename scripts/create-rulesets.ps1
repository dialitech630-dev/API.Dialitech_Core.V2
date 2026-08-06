# Creates GitHub Rulesets for API.Dialitech (main and develop)
# Requires: GitHub CLI (gh) authenticated with admin:org or repo admin rights.
#
# Usage:
#   powershell -File scripts/create-rulesets.ps1
#   powershell -File scripts/create-rulesets.ps1 -Owner dialitech630-dev -Repo API.Dialitech_Core.V2 -DryRun

param(
    [string]$Owner = "dialitech630-dev",
    [string]$Repo = "API.Dialitech_Core.V2",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not $DryRun) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI (gh) is not installed. Install it first: https://cli.github.com/"
    }

    gh auth status 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "gh is not authenticated. Run 'gh auth login' first."
    }
}

$requiredChecks = @(
    "build",
    "unit-tests",
    "integration-tests",
    "security-tests",
    "vulnerability-scan",
    "analyze",      # CodeQL job name
    "gitleaks"      # Secret Scan job name
)

$baseRules = @(
    @{ type = "deletion" },
    @{ type = "non_fast_forward" },
    @{
        type = "pull_request"
        parameters = @{
            required_approving_review_count = 1
            dismiss_stale_reviews_on_push   = $true
            require_code_owner_review       = $false
            require_last_push_approval      = $false
            required_review_thread_resolution = $true
        }
    },
    @{
        type = "required_status_checks"
        parameters = @{
            strict_required_status_checks_policy = $true
            required_checks = @(
                foreach ($check in $requiredChecks) { @{ context = $check } }
            )
        }
    }
)

$rulesets = @(
    @{
        name        = "Production - main"
        target      = "branch"
        enforcement = "active"
        conditions  = @{
            ref_name = @{
                include = @("refs/heads/main")
                exclude = @()
            }
        }
        rules       = $baseRules
    },
    @{
        name        = "Integration - develop"
        target      = "branch"
        enforcement = "active"
        conditions  = @{
            ref_name = @{
                include = @("refs/heads/develop")
                exclude = @()
            }
        }
        rules       = $baseRules
    }
)

foreach ($ruleset in $rulesets) {
    $json = $ruleset | ConvertTo-Json -Depth 10

    if ($DryRun) {
        Write-Host "=== DRY RUN: would create ruleset '$($ruleset.name)' ===" -ForegroundColor Cyan
        Write-Host $json
        Write-Host ""
        continue
    }

    Write-Host "Creating ruleset '$($ruleset.name)'..." -ForegroundColor Yellow
    $jsonFile = [System.IO.Path]::GetTempFileName()
    Set-Content -LiteralPath $jsonFile -Value $json -Encoding utf8

    gh api --method POST "repos/$Owner/$Repo/rulesets" --input $jsonFile
    if ($LASTEXITCODE -ne 0) {
        Remove-Item -LiteralPath $jsonFile -Force
        throw "Failed to create ruleset '$($ruleset.name)'. It may already exist."
    }
    Remove-Item -LiteralPath $jsonFile -Force
}

if ($DryRun) {
    Write-Host "Dry run finished. Review the JSON above, then run without -DryRun." -ForegroundColor Cyan
} else {
    Write-Host "Rulesets created successfully." -ForegroundColor Green
}
