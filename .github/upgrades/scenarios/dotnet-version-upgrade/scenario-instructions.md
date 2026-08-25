# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (.NET 10 LTS)

## Source Control
- **Source Branch**: product5
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages)

## Strategy
**Selected**: All-at-Once
**Rationale**: Small solution (3 projects), no CI-green constraint, no deep dependency graph — single atomic upgrade is most efficient.

### Execution Constraints
- Single atomic upgrade — all projects updated together
- Validate full solution build after upgrade
- Resolve Microsoft.Data.Sqlite (→10.0.11) and Microsoft.Graphics.Win2D (→1.1.0) inline during the upgrade task
