#!/usr/bin/env bash
# Builds CLLC Extended and the Hexium/Thunderstore package: dist/TeamVierge-CLLC_Extended-<version>.zip
# Usage: scripts/package.sh [extra dotnet build arguments, e.g. -p:ValheimDir=/path/to/Valheim]
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

version="$(sed -n 's/.*Version = "\([0-9.]*\)".*/\1/p' "$root/PluginInfo.cs")"
manifest_version="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["version_number"])' "$root/package/manifest.json")"
if [[ "$version" != "$manifest_version" ]]; then
  echo "Version mismatch: PluginInfo.cs has $version, package/manifest.json has $manifest_version" >&2
  exit 1
fi

dotnet build "$root/CLLC_Extended.csproj" -c Release -warnaserror "$@"

archive="$root/dist/TeamVierge-CLLC_Extended-$version.zip"
mkdir -p "$root/dist"
python3 - "$root" "$archive" <<'PY'
import sys
import zipfile
from pathlib import Path

root, archive = Path(sys.argv[1]), sys.argv[2]
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as zf:
    for name in ("manifest.json", "README.md", "CHANGELOG.md", "icon.png"):
        zf.write(root / "package" / name, name)
    zf.write(root / "LICENSE", "LICENSE")
    zf.write(root / "bin/Release/net472/CLLC_Extended.dll", "plugins/CLLC_Extended.dll")
PY

echo "Package: $archive"
