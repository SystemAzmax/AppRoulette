# .NET 10 Upgrade Plan

## Overview

**Target**: AppRoulette solution — upgrade from .NET 8 (WinForms/WinUI) to .NET 10
**Scope**: 3 projects, small solution (~few thousand LOC) — AppRoulette app, AppRoulette.Tests, AppRoulette (Package) packaging project

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 3 projects, small solution, no CI-green constraint, straightforward TFM bump with 2 known package replacements.

## Tasks

### 01-prerequisites: SDKとツールチェーンの確認

.NET 10 SDKがマシンにインストールされていることを確認し、global.jsonが存在する場合はSDKバージョンとの互換性を検証する。プロジェクトはWindows Desktop (WinForms/WinUI) をターゲットとしているため、Windows App SDK関連の依存関係も対象TFM (`net10.0-windows10.0.22000.0` / パッケージングプロジェクトは `net10.0-windows10.0.26100.0`) に対応していることを確認する。

**Done when**: .NET 10 SDKのインストールが確認され、global.jsonの互換性検証が完了している。

---

### 02-upgrade-all-projects: 全プロジェクトを.NET 10へアップグレード

AppRoulette.csproj (WinForms/WinUI, 現行 `net8.0-windows10.0.19041.0`)、AppRoulette.Tests.csproj (現行 `net8.0-windows10.0.19041.0`)、AppRoulette (Package).wapproj (現行 `net451`、レガシーパッケージングプロジェクト) の3プロジェクトを一括で対象TFMへ更新する。

主な作業内容:
- 各プロジェクトのTargetFrameworkを更新する(推奨: アプリ/テストは `net10.0-windows10.0.22000.0`、パッケージングプロジェクトは `net10.0-windows10.0.26100.0`)
- NuGetパッケージを更新する: `Microsoft.Data.Sqlite` 8.0.0 → 10.0.11、`Microsoft.Graphics.Win2D` 1.4.0 → 1.1.0 (互換性のため)
- `Windows.UI.Color` / `Windows.Foundation.Size` を使用する生成コード・アプリコード (`RouletteRenderer.cs` など) でのソース非互換なAPI利用について、.NET 10の破壊的変更に対応した修正を行う
- xunit 2.9.3 は非推奨扱いだが引き続き利用可能 — 今回のスコープでは維持する(将来的にv3移行を検討)
- wapprojはレガシーな.NET Framework (net451) 形式のため、Windows Application Packaging Projectとしての互換性を確認しながら移行する

**Done when**: 3プロジェクトすべてが対象TFMに更新され、パッケージ参照が更新され、既知のソース非互換なAPI呼び出しが解消されている。

---

### 03-final-validation: 最終検証

ソリューション全体をビルドし、すべてのコンパイルエラーを解消する。既存のユニットテスト (AppRoulette.Tests) を実行し、全てパスすることを確認する。ビルド後に残った警告があれば修正する。

**Done when**: ソリューションがエラー・警告なしでビルドされ、全テストが成功する。
