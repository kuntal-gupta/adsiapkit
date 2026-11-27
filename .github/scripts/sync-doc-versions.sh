#!/usr/bin/env bash
# Rewrites the install examples in the docs to a release version.
# Usage: sync-doc-versions.sh 2.1.0   (with or without the leading "v")
# Touches only the install snippets: git URLs ending in #vX.Y.Z, the "`#vX.Y.Z` tag pins"
# sentence, and the OpenUPM "com.puzzlecraftgames.adsiapkit": "X.Y.Z" dependency line.
set -euo pipefail

version="${1#v}"
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Not a release version: $1" >&2
  exit 1
fi

files=(README.md Documentation~/SetupGuide.md)
semver='[0-9]+\.[0-9]+\.[0-9]+'

sed -E -i \
  -e "s/adsiapkit\.git#v${semver}/adsiapkit.git#v${version}/g" \
  -e "s/\`#v${semver}\` tag pins/\`#v${version}\` tag pins/g" \
  -e "s/(\"com\.puzzlecraftgames\.adsiapkit\": \")${semver}\"/\1${version}\"/g" \
  "${files[@]}"
