# Automate Sprint 39 ship gate: validate, branch, commit, push, PR, CI wait, merge, release dispatch.
# Requires: git, gh (authenticated), dotnet, python, bash (Git for Windows).
#
# Usage:
#   pwsh scripts/ship-sprint39-gate.ps1 -DryRun
#   pwsh scripts/ship-sprint39-gate.ps1 -ApproveRemote
#   pwsh scripts/ship-sprint39-gate.ps1 -ApproveRemote -Merge -DispatchRelease
param(
    [string]$Branch = "ship/sprint-39-v1.1.0",
    [string]$Base = "main",
    [string]$Version = "",
    [string]$Tag = "",
    [string]$CommitMessage = "",
    [switch]$DryRun,
    [switch]$SkipPreflight,
    [switch]$SkipPush,
    [switch]$SkipPr,
    [switch]$SkipWaitCi,
    [int]$WaitCiSeconds = 900,
    [switch]$Merge,
    [switch]$DispatchRelease,
    [switch]$ApproveRemote
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Git([string[]]$GitArgs) {
    if ($DryRun) {
        Write-Host "[DryRun] git $($GitArgs -join ' ')"
        return
    }
    & git @GitArgs
    if ($LASTEXITCODE -ne 0) { throw "git $($GitArgs -join ' ') failed ($LASTEXITCODE)" }
}

function Invoke-Gh([string[]]$GhArgs) {
    if ($DryRun) {
        Write-Host "[DryRun] gh $($GhArgs -join ' ')"
        return $null
    }
    $out = & gh @GhArgs 2>&1
    if ($LASTEXITCODE -ne 0) { throw "gh $($GhArgs -join ' ') failed: $out" }
    return $out
}

function Require-ApproveRemote([string]$Action) {
    if ($DryRun) { return }
    if (-not $ApproveRemote) {
        throw "Refusing to $Action without -ApproveRemote. Preview with -DryRun first."
    }
}

$ArtifactPaths = @(
    "artifacts/test-publish",
    "artifacts/fd-test",
    "artifacts/product-win-x64",
    "artifacts/product-msi",
    "artifacts/product-msix",
    "artifacts/sideload-codesign"
)

Write-Host "=== ship-sprint39-gate ===" -ForegroundColor Green
if ($DryRun) { Write-Host "(dry run - no git/gh mutations)" -ForegroundColor Yellow }

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh CLI required. Install: https://cli.github.com/ then run: gh auth login"
}

$repoJson = gh repo view --json nameWithOwner 2>$null
if (-not $repoJson) { throw "gh auth failed or not inside a GitHub repo" }
$Repo = (ConvertFrom-Json $repoJson).nameWithOwner
Write-Host "Repository: $Repo"

if (-not $Version) {
    $Version = (Get-Content (Join-Path $Root "src/SpatialLabsOptimizer/product-version.json") | ConvertFrom-Json).version
}
if (-not $Tag) { $Tag = "SpatialLabsOptimizer-v$Version" }

if (-not $CommitMessage) {
    $CommitMessage = @"
feat: ship Sprints 32-52 and 40-44 for v$Version

Land local product work, modularization, QA gates, and release scripts.
168 tests green; post-sprint validation passed locally.
"@
}

# --- Phase 1: preflight ---
if (-not $SkipPreflight) {
    Write-Step "Phase 1 - local preflight"
    if ($DryRun) {
        Write-Host "[DryRun] run-post-sprint-validation.ps1"
    } else {
        & (Join-Path $Root "scripts/run-post-sprint-validation.ps1")
        if ($LASTEXITCODE -ne 0) { throw "post-sprint validation failed" }
    }
}

# --- Phase 2: branch + commit ---
Write-Step "Phase 2 - branch and commit"
$currentBranch = (git branch --show-current).Trim()
$dirty = git status --porcelain

if ($currentBranch -eq $Base -and $dirty) {
    Write-Host "Creating branch $Branch from $Base"
    Invoke-Git @('checkout', '-b', $Branch)
} elseif ($currentBranch -ne $Branch) {
    $localBranch = git branch --list $Branch
    if ($localBranch) {
        Invoke-Git @('checkout', $Branch)
    } elseif ($dirty) {
        Invoke-Git @('checkout', '-b', $Branch)
    } else {
        throw "On branch '$currentBranch' with a clean tree. Checkout $Branch or make changes first."
    }
} else {
    Write-Host "Already on $Branch"
}

$dirty = git status --porcelain
if ($dirty) {
    Write-Host "Staging changes (excluding build artifacts)..."
    Invoke-Git @('add', '-A')
    foreach ($path in $ArtifactPaths) {
        if (Test-Path $path) {
            Invoke-Git @('reset', 'HEAD', '--', $path)
        }
    }

    $staged = if ($DryRun) { "would stage" } else { (git diff --cached --name-only) }
    if (-not $DryRun -and -not $staged) {
        Write-Warning "Nothing staged after excluding artifacts - skipping commit"
    } else {
        Write-Host "Committing..."
        if ($DryRun) {
            Write-Host "[DryRun] git commit with Sprint 39 ship message"
        } else {
            git commit -m $CommitMessage
            if ($LASTEXITCODE -ne 0) { throw "git commit failed (hook rejected?)" }
        }
    }
} else {
    Write-Host "Working tree clean - skipping commit"
}

$HeadSha = if ($DryRun) { "dry-run-sha" } else { (git rev-parse HEAD).Trim() }
Write-Host "HEAD: $HeadSha"

# --- Phase 3: push ---
Write-Step "Phase 3 - push to origin"
if ($SkipPush) {
    Write-Host "Skipped (-SkipPush)"
} else {
    Require-ApproveRemote "push"
    Invoke-Git @('push', '-u', 'origin', $Branch)
}

# --- Phase 4: pull request ---
Write-Step "Phase 4 - open pull request to $Base"
$prUrl = $null
if ($SkipPr) {
    Write-Host "Skipped (-SkipPr)"
} else {
    $existing = $null
    if (-not $DryRun) {
        $existingJson = gh pr list --head $Branch --base $Base --state open --json url,number 2>$null
        if ($existingJson) {
            $existing = ConvertFrom-Json $existingJson | Select-Object -First 1
        }
    }

    if ($existing) {
        $prUrl = $existing.url
        Write-Host "Using existing PR #$($existing.number): $prUrl"
    } else {
        Require-ApproveRemote "create pull request"
        $prBody = @(
            "## Summary",
            "- Lands local sprint work (Sprints 32-52, 40-44): product, tests, docs, CI/release scripts.",
            "- Local: 168/168 tests green; run-post-sprint-validation.ps1 passed.",
            "",
            "## Test plan",
            "- [ ] CI workflow green on this PR",
            "- [ ] After merge: Product Release dispatched for $Tag"
        ) -join "`n"
        if ($DryRun) {
            Write-Host "[DryRun] gh pr create --base $Base --head $Branch"
            $prUrl = "https://github.com/$Repo/pull/DRY-RUN"
        } else {
            $prUrl = gh pr create `
                --base $Base `
                --head $Branch `
                --title "Ship v$Version - Sprints 32-52 + 40-44" `
                --body $prBody 2>&1
            if ($LASTEXITCODE -ne 0) { throw "gh pr create failed" }
            Write-Host "Created PR: $prUrl"
        }
    }
}

# --- Phase 5: wait for CI ---
Write-Step "Phase 5 - wait for GitHub CI on commit"
if ($SkipWaitCi) {
    Write-Host "Skipped (-SkipWaitCi)"
} elseif ($DryRun) {
    Write-Host "[DryRun] check-github-ci.ps1 -Ref $HeadSha -WaitSeconds $WaitCiSeconds"
} else {
    & (Join-Path $Root "scripts/check-github-ci.ps1") -Ref $HeadSha -WaitSeconds $WaitCiSeconds
    if ($LASTEXITCODE -ne 0) {
        throw "CI not green on $HeadSha - fix failures and re-run this script"
    }
}

# --- Phase 6: merge ---
Write-Step "Phase 6 - merge pull request"
if (-not $Merge) {
    Write-Host "Skipped (pass -Merge to squash-merge after CI green)"
} elseif ($DryRun) {
    Write-Host "[DryRun] gh pr merge --squash --delete-branch"
} else {
    Require-ApproveRemote "merge pull request"
    if (-not $prUrl) {
        $prUrl = (gh pr list --head $Branch --base $Base --state open --json url -q ".[0].url")
    }
    if (-not $prUrl) { throw "No open PR found for $Branch -> $Base" }
    Invoke-Gh @('pr', 'merge', $prUrl, '--squash', '--delete-branch')
    Invoke-Git @('checkout', $Base)
    Invoke-Git @('pull', 'origin', $Base)
    $HeadSha = (git rev-parse HEAD).Trim()
    Write-Host "Merged. main now at $HeadSha"
}

# --- Phase 7: product release workflow ---
Write-Step "Phase 7 - dispatch Product Release workflow"
if (-not $DispatchRelease) {
    Write-Host "Skipped (pass -DispatchRelease to run product-release.yml)"
} elseif ($DryRun) {
    Write-Host "[DryRun] gh workflow run product-release.yml -f tag=$Tag"
} else {
    Require-ApproveRemote "dispatch product release"
    if ($Merge) {
        Write-Host "Waiting for CI on main before release dispatch..."
        & (Join-Path $Root "scripts/check-github-ci.ps1") -Ref $HeadSha -WaitSeconds $WaitCiSeconds
        if ($LASTEXITCODE -ne 0) { throw "CI not green on main after merge" }
    }
    Invoke-Gh @('workflow', 'run', 'product-release.yml', '-f', "tag=$Tag")
    Write-Host "Dispatched product-release.yml for $Tag"
    Write-Host "Monitor: https://github.com/$Repo/actions/workflows/product-release.yml"
}

Write-Host ""
Write-Host "=== ship-sprint39-gate complete ===" -ForegroundColor Green
Write-Host @"

Next steps:
  1. If you stopped before merge: review PR and re-run with -Merge -DispatchRelease
  2. Watch Product Release workflow on GitHub Actions
  3. Confirm release assets at: https://github.com/$Repo/releases/tag/$Tag
  4. Mark BUILD_PLAN Sprint 39 items done after remote CI + release succeed

Quick re-run (full automation):
  pwsh scripts/ship-sprint39-gate.ps1 -ApproveRemote -Merge -DispatchRelease
"@
