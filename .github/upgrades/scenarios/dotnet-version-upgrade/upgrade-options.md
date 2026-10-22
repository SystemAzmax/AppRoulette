# Upgrade Options — AppRoulette

Assessment: 3 projects (AppRoulette WinForms/WinUI app on net8.0-windows10.0.19041.0, AppRoulette.Tests on net8.0-windows10.0.19041.0, AppRoulette (Package).wapproj packaging project on net451) targeting net10.0; 2 incompatible NuGet packages, several source-incompatible API usages (Windows.UI.Color / Windows.Foundation.Size in generated XAML code).

## Strategy

### Upgrade Strategy
All 3 projects are small in count (≤15) with limited high-risk migrations and no CI-green constraint, so a single atomic upgrade is the most efficient approach.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade all 3 projects together in one pass; validate full solution build after upgrade. |
| Top-Down | Upgrade the app first, then tests/packaging — adds overhead not needed for this small solution. |

## Compatibility

### Unsupported Packages
2 packages need attention: `Microsoft.Data.Sqlite` 8.0.0 (upgrade to 10.0.11) and `Microsoft.Graphics.Win2D` 1.4.0 (recommended downgrade to 1.1.0 for compatibility) — small enough to resolve inline.

| Value | Description |
|-------|-------------|
| **Resolve Inline** (selected) | Update package references directly within the upgrade task (Sqlite → 10.0.11, Win2D → 1.1.0), no deferred follow-up tasks. |
| Defer Resolution | Stub out packages and create follow-up tasks — unnecessary given only 2 packages with known replacements. |
