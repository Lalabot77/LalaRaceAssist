using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LaunchPlugin
{
    public enum LeagueClassSource
    {
        None = 0,
        Csv = 1,
        Name = 2,
        Manual = 3,
        Native = 4
    }

    public enum LeagueClassMode
    {
        Disabled = 0,
        CsvOnly = 1,
        NameOnly = 2,
        CsvThenName = 3
    }

    public sealed class EffectiveRaceClassInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public string ColourHex { get; set; } = string.Empty;
        public bool Valid { get; set; }
        public LeagueClassSource Source { get; set; } = LeagueClassSource.None;

        public static EffectiveRaceClassInfo Invalid(LeagueClassSource source = LeagueClassSource.None)
        {
            return new EffectiveRaceClassInfo { Source = source, Valid = false };
        }
    }

    public sealed class LeagueClassCsvEntry
    {
        public int CustomerId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string ColourHex { get; set; } = string.Empty;
        public int Rank { get; set; }
    }

    public sealed class LeagueClassFallbackRule
    {
        public bool Enabled { get; set; }
        public string MatchSuffix { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public string ColourHex { get; set; } = string.Empty;
    }

    public sealed class LeagueClassStatus
    {
        public string ConfigStatusText { get; set; } = "Disabled";
        public int LoadedCount { get; set; }
        public int InvalidRowCount { get; set; }
        public int DuplicateRowCount { get; set; }
        public int ValidDriverCount { get; set; }
        public IReadOnlyList<string> DetectedClasses { get; set; } = Array.Empty<string>();
        public IReadOnlyList<LeagueClassFallbackRule> DetectedClassRows { get; set; } = Array.Empty<LeagueClassFallbackRule>();
    }

    public sealed class LeagueClassResolver
    {
        private readonly Dictionary<int, LeagueClassCsvEntry> _csvByCustomerId = new Dictionary<int, LeagueClassCsvEntry>();
        private readonly List<string> _detectedClassNames = new List<string>();
        private LeagueClassStatus _status = new LeagueClassStatus();

        public LeagueClassStatus Status => _status;

        public void Reload(LaunchPluginSettings settings)
        {
            _csvByCustomerId.Clear();
            _detectedClassNames.Clear();

            if (settings == null || !settings.LeagueClassEnabled)
            {
                _status = new LeagueClassStatus { ConfigStatusText = "Disabled" };
                return;
            }

            int loaded = 0;
            int invalid = 0;
            int duplicate = 0;

            var mode = (LeagueClassMode)settings.LeagueClassMode;
            bool csvMode = mode == LeagueClassMode.CsvOnly || mode == LeagueClassMode.CsvThenName;
            string path = settings.LeagueClassCsvPath ?? string.Empty;

            string readError = string.Empty;
            if (csvMode && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    foreach (string rawLine in File.ReadAllLines(path))
                    {
                        string line = (rawLine ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split(';');
                        if (parts.Length < 4)
                        {
                            invalid++;
                            continue;
                        }

                        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int customerId))
                        {
                            if (parts[0].Trim().Equals("CustomerId", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            invalid++;
                            continue;
                        }

                        string className = (parts[1] ?? string.Empty).Trim();
                        string colourHex = NormalizeHex(parts[2]);
                        if (!int.TryParse((parts[3] ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank))
                        {
                            invalid++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(className) || rank <= 0)
                        {
                            invalid++;
                            continue;
                        }

                        var entry = new LeagueClassCsvEntry
                        {
                            CustomerId = customerId,
                            ClassName = className,
                            ColourHex = colourHex,
                            Rank = rank
                        };

                        if (_csvByCustomerId.ContainsKey(customerId)) duplicate++;
                        _csvByCustomerId[customerId] = entry;
                        loaded++;

                        if (!_detectedClassNames.Contains(className, StringComparer.OrdinalIgnoreCase))
                        {
                            _detectedClassNames.Add(className);
                        }
                    }
                }
                catch (Exception ex)
                {
                    readError = ex.Message;
                    loaded = 0;
                    invalid = 0;
                    duplicate = 0;
                    _csvByCustomerId.Clear();
                    _detectedClassNames.Clear();
                }
            }

            _status = new LeagueClassStatus
            {
                ConfigStatusText = BuildStatusText(settings, path, csvMode, readError),
                LoadedCount = loaded,
                InvalidRowCount = invalid,
                DuplicateRowCount = duplicate,
                ValidDriverCount = _csvByCustomerId.Count,
                DetectedClasses = _detectedClassNames.ToArray(),
                DetectedClassRows = _detectedClassNames
                    .Select(name =>
                    {
                        var first = _csvByCustomerId.Values.FirstOrDefault(v => string.Equals(v.ClassName, name, StringComparison.OrdinalIgnoreCase));
                        return new LeagueClassFallbackRule
                        {
                            Enabled = true,
                            MatchSuffix = string.Empty,
                            ClassName = name,
                            ShortName = name,
                            Rank = first?.Rank ?? 0,
                            ColourHex = first?.ColourHex ?? string.Empty
                        };
                    })
                    .ToArray()
            };
        }

        public EffectiveRaceClassInfo ResolvePlayerPreview(LaunchPluginSettings settings, int? playerCustomerId, string playerName)
        {
            if (settings == null || !settings.LeagueClassEnabled)
            {
                return EffectiveRaceClassInfo.Invalid(LeagueClassSource.None);
            }

            if (settings.LeagueClassPlayerOverrideMode == 1)
            {
                return new EffectiveRaceClassInfo
                {
                    Name = settings.LeagueClassPlayerOverrideClassName ?? string.Empty,
                    ShortName = settings.LeagueClassPlayerOverrideShortName ?? string.Empty,
                    Rank = settings.LeagueClassPlayerOverrideRank,
                    ColourHex = NormalizeHex(settings.LeagueClassPlayerOverrideColourHex),
                    Source = LeagueClassSource.Manual,
                    Valid = !string.IsNullOrWhiteSpace(settings.LeagueClassPlayerOverrideClassName)
                };
            }

            var mode = (LeagueClassMode)settings.LeagueClassMode;
            bool checkCsvFirst = mode == LeagueClassMode.CsvOnly || mode == LeagueClassMode.CsvThenName;
            bool checkName = mode == LeagueClassMode.NameOnly || mode == LeagueClassMode.CsvThenName;

            if (checkCsvFirst && playerCustomerId.HasValue && _csvByCustomerId.TryGetValue(playerCustomerId.Value, out var csv))
            {
                return new EffectiveRaceClassInfo
                {
                    Name = csv.ClassName,
                    ShortName = csv.ClassName,
                    Rank = csv.Rank,
                    ColourHex = csv.ColourHex,
                    Source = LeagueClassSource.Csv,
                    Valid = true
                };
            }

            if (checkName)
            {
                foreach (var rule in settings.LeagueClassFallbackRules ?? new List<LeagueClassFallbackRule>())
                {
                    if (!rule.Enabled) continue;
                    string suffix = (rule.MatchSuffix ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(suffix)) continue;
                    if ((playerName ?? string.Empty).Trim().EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        return new EffectiveRaceClassInfo
                        {
                            Name = rule.ClassName ?? string.Empty,
                            ShortName = rule.ShortName ?? string.Empty,
                            Rank = rule.Rank,
                            ColourHex = NormalizeHex(rule.ColourHex),
                            Source = LeagueClassSource.Name,
                            Valid = !string.IsNullOrWhiteSpace(rule.ClassName)
                        };
                    }
                }
            }

            return EffectiveRaceClassInfo.Invalid(LeagueClassSource.Native);
        }

        private static string BuildStatusText(LaunchPluginSettings settings, string path, bool csvMode, string readError)
        {
            if (!settings.LeagueClassEnabled) return "Disabled";
            if (!csvMode) return "Name fallback mode active";
            if (string.IsNullOrWhiteSpace(path)) return "CSV path not set";
            if (!File.Exists(path)) return "CSV file not found";
            if (!string.IsNullOrWhiteSpace(readError)) return "CSV read error: " + readError;
            return "CSV loaded";
        }

        private static string NormalizeHex(string value)
        {
            string input = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            if (input.StartsWith("#")) input = input.Substring(1);
            if (input.Length == 6) return "#" + input.ToUpperInvariant();
            return string.Empty;
        }
    }
}
