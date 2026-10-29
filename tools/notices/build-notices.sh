#!/usr/bin/env bash
# ThirdPartyNotices.txt を、NuGet パッケージ付属のライセンスファイルなどから作成する。
# NuGet パッケージや .NET のランタイムを更新したら、下のバージョンを書き換えてから実行する
# （詳しくは docs/BuildGuide.md の「ライセンス表示の更新」を参照）。
#
# 使い方（Git Bash で、リポジトリのフォルダーから実行する）:
#   bash tools/notices/build-notices.sh [出力先] [ライセンスファイルのフォルダー]
# 出力先の既定は AppRoulette/AppRoulette/ThirdPartyNotices.txt。
# ライセンスファイルのフォルダーの既定は tools/notices/licenses
# （パッケージにライセンスファイルが無い Microsoft.Data.Sqlite と SQLitePCLRaw の LICENSE を置いている）。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${1:-$SCRIPT_DIR/../../AppRoulette/AppRoulette/ThirdPartyNotices.txt}"
DL="${2:-$SCRIPT_DIR/licenses}"
P="$(cygpath -u "$USERPROFILE")/.nuget/packages"

# 改行を LF にそろえ、末尾に改行を付けて出力する
emit_file() {
  tr -d '\r' < "$1"
  printf '\n'
}

section() {
  printf '\n\n================================================================================\n'
  printf '%s\n' "$1"
  printf '================================================================================\n\n'
}

{
  printf '\xEF\xBB\xBF'
  cat <<'EOF'
AppRoulette サードパーティ ソフトウェアのライセンス情報
================================================================================

AppRoulette は、次のソフトウェアを使用しています。
各ソフトウェアの著作権表示とライセンスを、原文のまま記載します。

  1. .NET Community Toolkit (CommunityToolkit.Mvvm 8.4.2) - MIT License
  2. Microsoft.Data.Sqlite 10.0.11 - MIT License
  3. SQLitePCLRaw 2.1.12 - Apache License 2.0
  4. SQLite - Public Domain
  5. .NET 10（.NET ランタイム）- MIT License
  6. Microsoft Edge WebView2 SDK 1.0.3719.77 - BSD 3-Clause 形式のライセンス
  7. Win2D (Microsoft.Graphics.Win2D 1.4.0) - Microsoft ソフトウェア ライセンス条項
  8. Windows App SDK 2.6.4（WinUI などの部品）- Microsoft ソフトウェア ライセンス条項
  9. Windows SDK の .NET 向けプロジェクション (Microsoft.Windows.SDK.NET.Ref 10.0.22000.57)
     - Microsoft ソフトウェア ライセンス条項

付録として、.NET Community Toolkit、Microsoft Edge WebView2 SDK、.NET、
Windows App SDK に含まれるサードパーティのソフトウェアの表示を、
各ソフトウェアに付属のファイルから原文のまま記載します。

  付録 A. .NET Community Toolkit - ThirdPartyNotices.txt
  付録 B. Microsoft Edge WebView2 SDK - NOTICE.txt
  付録 C. .NET - THIRD-PARTY-NOTICES.TXT
  付録 D. Windows App SDK - NOTICE.txt
EOF

  section "1. .NET Community Toolkit (CommunityToolkit.Mvvm 8.4.2) - MIT License"
  emit_file "$P/communitytoolkit.mvvm/8.4.2/License.md"

  section "2. Microsoft.Data.Sqlite 10.0.11 - MIT License"
  emit_file "$DL/efcore-LICENSE.txt"

  section "3. SQLitePCLRaw 2.1.12 - Apache License 2.0"
  printf 'Copyright 2014-2024 SourceGear, LLC\n'
  emit_file "$DL/sqlitepclraw-LICENSE.TXT"

  section "4. SQLite - Public Domain"
  cat <<'EOF'
SQLite is in the Public Domain.
https://www.sqlite.org/copyright.html
EOF

  section "5. .NET 10 (.NET Runtime) - MIT License"
  emit_file "$P/microsoft.netcore.app.runtime.win-x64/10.0.13/LICENSE.TXT"

  section "6. Microsoft Edge WebView2 SDK 1.0.3719.77 - BSD 3-Clause style license"
  emit_file "$P/microsoft.web.webview2/1.0.3719.77/LICENSE.txt"

  section "7. Win2D (Microsoft.Graphics.Win2D 1.4.0) - Microsoft Software License Terms"
  cat <<'EOF'
Copyright (c) Microsoft Corporation. All rights reserved.
Redistributed under the Microsoft Software License Terms for Win2D.
http://www.microsoft.com/web/webpi/eula/eula_win2d_10012014.htm
EOF

  section "8. Windows App SDK 2.6.4 - Microsoft Software License Terms"
  cat <<'EOF'
Copyright (c) Microsoft Corporation. All rights reserved.
Redistributed under the Microsoft Software License Terms for Microsoft Windows App SDK.
Third-party notices for the Windows App SDK are included in Appendix D.
EOF

  section "9. Windows SDK .NET Projection (Microsoft.Windows.SDK.NET.Ref 10.0.22000.57) - Microsoft Software License Terms"
  cat <<'EOF'
Copyright (c) Microsoft Corporation. All rights reserved.
Redistributed under the Microsoft Software License Terms for the Windows SDK.
https://aka.ms/WinSDKLicenseURL
EOF

  section "付録 A. .NET Community Toolkit - ThirdPartyNotices.txt"
  emit_file "$P/communitytoolkit.mvvm/8.4.2/ThirdPartyNotices.txt"

  section "付録 B. Microsoft Edge WebView2 SDK - NOTICE.txt"
  emit_file "$P/microsoft.web.webview2/1.0.3719.77/NOTICE.txt"

  section "付録 C. .NET - THIRD-PARTY-NOTICES.TXT"
  emit_file "$P/microsoft.netcore.app.runtime.win-x64/10.0.13/THIRD-PARTY-NOTICES.TXT"

  section "付録 D. Windows App SDK - NOTICE.txt"
  # 部品のうち Base の NOTICE.txt が SDK 全体の表示（WinUI の NOTICE.txt の内容も含む）
  emit_file "$P/microsoft.windowsappsdk.base/2.6.4/NOTICE.txt"
} | sed 's/$/\r/' > "$OUT"  # Windows で開くファイルのため、改行を CRLF にして書き出す
