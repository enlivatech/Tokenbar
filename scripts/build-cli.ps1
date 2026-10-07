# Builds the Win-CodexBar Rust CLI that Tokenbar uses for all provider data.
# Output: vendor\Win-CodexBar\target\release\codexbar.exe (CliLocator looks there in dev builds).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$vendor = Join-Path $root 'vendor\Win-CodexBar'
$target = Join-Path $vendor 'target'

if (-not (Test-Path (Join-Path $vendor 'Cargo.toml'))) {
    git -C $root submodule update --init --depth 1 vendor/Win-CodexBar
}

cargo build --release -p codexbar --bin codexbar --manifest-path (Join-Path $vendor 'Cargo.toml') --target-dir $target
if ($LASTEXITCODE -ne 0) { throw "cargo build failed ($LASTEXITCODE)" }
Write-Output (Join-Path $target 'release\codexbar.exe')
