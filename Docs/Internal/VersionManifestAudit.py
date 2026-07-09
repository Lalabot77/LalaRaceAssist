#!/usr/bin/env python3
"""Release-prep audit for LalaRaceAssist.VersionManifest.json.

Reports plugin assembly version alignment and dashboard/overlay DashboardVersion
metadata alignment from official SimHub .simhubdash export packages. This script
is read-only and never edits files or extracts packages.
"""
from __future__ import print_function

import json
import re
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "LalaRaceAssist.VersionManifest.json"
ASSEMBLY_INFO = ROOT / "Properties" / "AssemblyInfo.cs"
PROJECT_FILE = ROOT / "LaunchPlugin.csproj"
REQUIRED_ASSETS = (
    "Lala-Driver Dash",
    "Lala-Strategy Dash",
    "Lala-Alerts Overlay",
    "Lala-VerticalTrafficBar Overlay",
    "Lala-Head2Head",
    "Lala-Fuel Calculator",
)
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


def manifest_is_embedded_resource():
    ns = {"msb": "http://schemas.microsoft.com/developer/msbuild/2003"}
    tree = ET.parse(str(PROJECT_FILE))
    for item in tree.findall(".//msb:EmbeddedResource", ns):
        if item.attrib.get("Include") == "LalaRaceAssist.VersionManifest.json":
            return True
    return False


def normalized_archive_name(value):
    return str(value or "").replace("\\", "/").strip("/")


def archive_basename(value):
    return normalized_archive_name(value).split("/")[-1]


def read_dashboard_json_from_package(package_path, root_file):
    root_basename = archive_basename(root_file)
    if not root_basename:
        raise ValueError("releaseAudit.rootFile is blank")

    with zipfile.ZipFile(str(package_path), "r") as archive:
        matches = []
        for name in archive.namelist():
            normalized = normalized_archive_name(name)
            if normalized.lower().endswith(".djson") and archive_basename(normalized).lower() == root_basename.lower():
                matches.append(name)

        if not matches:
            raise FileNotFoundError("root .djson missing: %s" % root_basename)
        if len(matches) > 1:
            raise RuntimeError("root .djson ambiguous for %s: %s" % (root_basename, ", ".join(matches)))

        member_name = matches[0]
        raw = archive.read(member_name)
        text = raw.decode("utf-8-sig")
        return member_name, json.loads(text)


def main():
    issues = {"critical": [], "noncritical": []}

    manifest = read_json(MANIFEST)
    assembly = read_assembly_versions()
    plugin = manifest.get("plugin") or {}
    assets = manifest.get("assets") or {}
    if not isinstance(plugin, dict):
        plugin = {}
    if not isinstance(assets, dict):
        assets = {}

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

    embedded = manifest_is_embedded_resource()
    print("Project embedded manifest resource:")
    print("  LalaRaceAssist.VersionManifest.json embedded: %s" % ("yes" if embedded else "no"))
    print("")
    if not embedded:
        add_issue(issues, True, "LalaRaceAssist.VersionManifest.json is not an EmbeddedResource in LaunchPlugin.csproj")

    if not isinstance(manifest.get("plugin"), dict):
        add_issue(issues, True, "manifest plugin section is missing or not an object")
    if not isinstance(manifest.get("assets"), dict):
        add_issue(issues, True, "manifest assets section is missing or not an object")
    if not str(manifest.get("releaseFamily", "")).strip():
        add_issue(issues, True, "manifest releaseFamily is missing")

    if plugin.get("assemblyVersion") != assembly.get("AssemblyVersion"):
        add_issue(issues, True, "plugin.assemblyVersion does not match AssemblyVersion")
    if plugin.get("informationalVersion") != assembly.get("AssemblyInformationalVersion"):
        add_issue(issues, True, "plugin.informationalVersion does not match AssemblyInformationalVersion")
    if normalize_version(plugin.get("latest")) != normalize_version(assembly.get("AssemblyInformationalVersion")):
        add_issue(issues, True, "plugin.latest does not match AssemblyInformationalVersion")
    for field in ("latest", "minimumSupported", "assemblyVersion", "informationalVersion"):
        if not is_parseable_version(plugin.get(field)):
            add_issue(issues, True, "plugin.%s is missing or unparsable: %r" % (field, plugin.get(field)))

    for required_asset in REQUIRED_ASSETS:
        if required_asset not in assets:
            add_issue(issues, True, "required manifest asset is missing: %s" % required_asset)

    for asset_name in assets:
        if asset_name not in REQUIRED_ASSETS:
            add_issue(issues, True, "unrecognised manifest asset key: %s" % asset_name)

    print("Dashboard/overlay export packages:")
    for asset_name, asset in assets.items():
        if not isinstance(asset, dict):
            add_issue(issues, True, "%s asset schema is not an object" % asset_name)
            continue

        release_critical_value = asset.get("releaseCritical")
        release_critical_valid = isinstance(release_critical_value, bool)
        critical = release_critical_value if release_critical_valid else True
        latest = asset.get("latest", "")
        display_name = asset.get("displayName", "")
        family = asset.get("compatiblePluginFamily") if asset.get("compatiblePluginFamily") is not None else manifest.get("releaseFamily")
        folder = asset.get("folder", "")
        file_name = asset.get("file", "")
        version_property = asset.get("versionProperty", "")
        release_audit = asset.get("releaseAudit")
        package_name = ""
        root_file = ""
        if isinstance(release_audit, dict):
            package_name = release_audit.get("package", "")
            root_file = release_audit.get("rootFile", "")

        label = "release-critical" if critical else "non-critical"
        print("  %s [%s]" % (asset_name, label))
        print("    manifest latest: %s" % latest)

        display_name_valid = isinstance(display_name, str) and bool(display_name.strip())
        family_valid = isinstance(family, str) and bool(family.strip())
        folder_valid = isinstance(folder, str) and bool(folder.strip())
        file_valid = isinstance(file_name, str) and bool(file_name.strip())
        version_property_valid = isinstance(version_property, str) and bool(version_property.strip())
        release_audit_valid = isinstance(release_audit, dict)
        package_valid = isinstance(package_name, str) and bool(package_name.strip())
        root_file_valid = isinstance(root_file, str) and bool(root_file.strip())

        if not display_name_valid:
            add_issue(issues, True, "%s displayName is missing or not a non-empty string" % asset_name)
        if not family_valid:
            add_issue(issues, True, "%s compatible plugin family is missing or not a non-empty string" % asset_name)
        if not folder_valid:
            add_issue(issues, True, "%s folder is missing or not a non-empty string" % asset_name)
        if not file_valid:
            add_issue(issues, True, "%s file is missing or not a non-empty string" % asset_name)
        if not version_property_valid:
            add_issue(issues, True, "%s versionProperty is missing or not a non-empty string" % asset_name)
        if not release_critical_valid:
            add_issue(issues, True, "%s releaseCritical is missing or not boolean" % asset_name)
        if not is_parseable_version(latest):
            add_issue(issues, True, "%s manifest latest is missing or unparsable: %r" % (asset_name, latest))
        if not release_audit_valid:
            add_issue(issues, True, "%s releaseAudit is missing or not an object" % asset_name)
        if release_audit_valid and not package_valid:
            add_issue(issues, True, "%s releaseAudit.package is missing or blank" % asset_name)
        if release_audit_valid and not root_file_valid:
            add_issue(issues, True, "%s releaseAudit.rootFile is missing or blank" % asset_name)

        if not (package_valid and root_file_valid and version_property_valid):
            print("    package: INVALID SCHEMA")
            print("")
            continue

        package_path = ROOT / package_name
        print("    package: %s" % package_path.relative_to(ROOT))
        print("    rootFile: %s" % root_file)

        if not package_path.exists():
            add_issue(issues, critical, "%s package file is missing: %s" % (asset_name, package_path.relative_to(ROOT)))
            print("")
            continue
        if not zipfile.is_zipfile(str(package_path)):
            add_issue(issues, critical, "%s package is not zip-compatible: %s" % (asset_name, package_path.relative_to(ROOT)))
            print("")
            continue

        try:
            member_name, dash_json = read_dashboard_json_from_package(package_path, root_file)
        except FileNotFoundError as exc:
            add_issue(issues, critical, "%s root .djson missing: %s" % (asset_name, exc))
            print("")
            continue
        except RuntimeError as exc:
            add_issue(issues, critical, "%s root .djson ambiguous: %s" % (asset_name, exc))
            print("")
            continue
        except json.JSONDecodeError as exc:
            add_issue(issues, critical, "%s root .djson invalid JSON: %s" % (asset_name, exc))
            print("")
            continue
        except UnicodeDecodeError as exc:
            add_issue(issues, critical, "%s root .djson is not UTF-8 decodable: %s" % (asset_name, exc))
            print("")
            continue
        except zipfile.BadZipFile as exc:
            add_issue(issues, critical, "%s package is not zip-compatible: %s" % (asset_name, exc))
            print("")
            continue
        except Exception as exc:
            add_issue(issues, critical, "%s package/root .djson could not be read: %s" % (asset_name, exc))
            print("")
            continue

        print("    archive member: %s" % member_name)

        metadata = dash_json.get("Metadata") if isinstance(dash_json, dict) else None
        if not isinstance(metadata, dict):
            add_issue(issues, critical, "%s Metadata is missing or not an object in %s" % (asset_name, member_name))
            print("    %s path: MISSING" % version_property)
            print("")
            continue

        value = metadata.get(version_property)
        print("    %s path: $.Metadata.%s" % (version_property, version_property))
        print("    %s value: %s" % (version_property, value))
        if str(value or "").strip() == "":
            add_issue(issues, critical, "%s %s is missing or blank at $.Metadata.%s" % (asset_name, version_property, version_property))
        elif not is_parseable_version(value):
            add_issue(issues, critical, "%s %s is unparsable at $.Metadata.%s: %r" % (asset_name, version_property, version_property, value))
        elif normalize_version(value) != normalize_version(latest):
            add_issue(issues, critical, "%s package %s %r does not match manifest latest %r" % (asset_name, version_property, value, latest))
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
