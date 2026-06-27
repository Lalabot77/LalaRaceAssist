#!/usr/bin/env python3
"""Release-prep audit for LalaRaceAssist.VersionManifest.json.

Reports plugin assembly version alignment and root dashboard/overlay DashboardVersion
metadata alignment. This script is read-only and never edits files.
"""
from __future__ import print_function

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "LalaRaceAssist.VersionManifest.json"
ASSEMBLY_INFO = ROOT / "Properties" / "AssemblyInfo.cs"
VERSION_RE = re.compile(r"^v?\d+(?:\.\d+){1,3}$")


def normalize_version(value):
    if value is None:
        return ""
    text = str(value).strip()
    return text[1:] if text.lower().startswith("v") else text


def is_parseable_version(value):
    return bool(VERSION_RE.match(str(value or "").strip()))


def read_json(path):
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def find_key_paths(value, key_name, path="$"):
    found = []
    if isinstance(value, dict):
        for key, child in value.items():
            child_path = path + "." + key
            if key == key_name:
                found.append((child_path, child))
            found.extend(find_key_paths(child, key_name, child_path))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            found.extend(find_key_paths(child, key_name, "%s[%d]" % (path, index)))
    return found


def read_assembly_versions():
    text = ASSEMBLY_INFO.read_text(encoding="utf-8-sig")
    versions = {}
    for name in ("AssemblyVersion", "AssemblyFileVersion", "AssemblyInformationalVersion"):
        match = re.search(r"\[assembly:\s*%s\(\"([^\"]+)\"\)\]" % name, text)
        versions[name] = match.group(1) if match else ""
    return versions


def add_issue(issues, critical, message):
    if critical:
        issues["critical"].append(message)
    else:
        issues["noncritical"].append(message)


def main():
    issues = {"critical": [], "noncritical": []}

    manifest = read_json(MANIFEST)
    assembly = read_assembly_versions()
    plugin = manifest.get("plugin") or {}
    assets = manifest.get("assets") or {}

    print("Lala Race Assist version manifest audit")
    print("Manifest: %s" % MANIFEST.relative_to(ROOT))
    print("")

    print("Plugin assembly versions:")
    print("  AssemblyVersion: %s" % assembly.get("AssemblyVersion", ""))
    print("  AssemblyFileVersion: %s" % assembly.get("AssemblyFileVersion", ""))
    print("  AssemblyInformationalVersion: %s" % assembly.get("AssemblyInformationalVersion", ""))
    print("")

    print("Manifest plugin versions:")
    print("  plugin.latest: %s" % plugin.get("latest", ""))
    print("  plugin.assemblyVersion: %s" % plugin.get("assemblyVersion", ""))
    print("  plugin.informationalVersion: %s" % plugin.get("informationalVersion", ""))
    print("")

    if plugin.get("assemblyVersion") != assembly.get("AssemblyVersion"):
        add_issue(issues, True, "plugin.assemblyVersion does not match AssemblyVersion")
    if plugin.get("informationalVersion") != assembly.get("AssemblyInformationalVersion"):
        add_issue(issues, True, "plugin.informationalVersion does not match AssemblyInformationalVersion")
    if normalize_version(plugin.get("latest")) != normalize_version(assembly.get("AssemblyInformationalVersion")):
        add_issue(issues, True, "plugin.latest does not match AssemblyInformationalVersion")
    for field in ("latest", "minimumSupported", "assemblyVersion", "informationalVersion"):
        if not is_parseable_version(plugin.get(field)):
            add_issue(issues, True, "plugin.%s is missing or unparsable: %r" % (field, plugin.get(field)))

    print("Dashboard/overlay assets:")
    for asset_name, asset in assets.items():
        critical = bool(asset.get("releaseCritical"))
        latest = asset.get("latest", "")
        folder = asset.get("folder", "")
        file_name = asset.get("file", "")
        version_property = asset.get("versionProperty", "DashboardVersion")
        dash_path = ROOT / folder / file_name
        label = "release-critical" if critical else "non-critical"
        print("  %s [%s]" % (asset_name, label))
        print("    manifest latest: %s" % latest)
        print("    file: %s" % dash_path.relative_to(ROOT))

        if not is_parseable_version(latest):
            add_issue(issues, critical, "%s manifest latest is missing or unparsable: %r" % (asset_name, latest))

        if not dash_path.exists():
            add_issue(issues, critical, "%s file is missing: %s" % (asset_name, dash_path.relative_to(ROOT)))
            continue

        try:
            dash_json = read_json(dash_path)
        except Exception as exc:
            add_issue(issues, critical, "%s JSON is unparsable: %s" % (asset_name, exc))
            continue

        paths = find_key_paths(dash_json, version_property)
        if not paths:
            add_issue(issues, critical, "%s has no %s metadata" % (asset_name, version_property))
            print("    %s path: MISSING" % version_property)
            continue
        if len(paths) > 1:
            add_issue(issues, critical, "%s has multiple %s metadata fields" % (asset_name, version_property))

        for path, value in paths:
            print("    %s path: %s" % (version_property, path))
            print("    %s value: %s" % (version_property, value))
            if str(value or "").strip() == "":
                add_issue(issues, critical, "%s %s is blank at %s" % (asset_name, version_property, path))
            elif not is_parseable_version(value):
                add_issue(issues, critical, "%s %s is unparsable at %s: %r" % (asset_name, version_property, path, value))
            elif normalize_version(value) != normalize_version(latest):
                add_issue(issues, critical, "%s %s %r does not match manifest latest %r" % (asset_name, version_property, value, latest))
        print("")

    print("Issue summary:")
    if not issues["critical"] and not issues["noncritical"]:
        print("  PASS: no missing, blank, mismatched, or unparsable versions found.")
        return 0

    if issues["critical"]:
        print("  Release-critical mismatches:")
        for issue in issues["critical"]:
            print("    - %s" % issue)
    else:
        print("  Release-critical mismatches: none")

    if issues["noncritical"]:
        print("  Non-critical mismatches:")
        for issue in issues["noncritical"]:
            print("    - %s" % issue)
    else:
        print("  Non-critical mismatches: none")

    return 1


if __name__ == "__main__":
    sys.exit(main())
