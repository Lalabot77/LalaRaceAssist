using System;
using Newtonsoft.Json;

namespace LaunchPlugin
{
    [JsonConverter(typeof(PitServiceRegulationJsonConverter))]
    public enum PitServiceRegulation
    {
        DefaultSequential = 0,
        ImsaSimultaneous = 1,
        NecSimultaneous = 2
    }

    public sealed class PitServiceTimeResult
    {
        public PitServiceRegulation Regulation { get; set; }
        public double FuelSeconds { get; set; }
        public double TyreSeconds { get; set; }
        public double CoreServiceSeconds { get; set; }
        public double ServiceSecondsWithOverhead { get; set; }
        public double EffectiveRefuelRateLps { get; set; }
        public double NecFactorPercentUsed { get; set; }
    }

    public static class PitServiceTimeModel
    {
        public const double DefaultNecRefuelRatePercent = 100.0;
        public const double MinNecRefuelRatePercent = 20.0;
        public const double MaxNecRefuelRatePercent = 120.0;

        public static PitServiceRegulation NormalizeRegulation(PitServiceRegulation regulation)
        {
            return Enum.IsDefined(typeof(PitServiceRegulation), regulation)
                ? regulation
                : PitServiceRegulation.DefaultSequential;
        }

        public static PitServiceRegulation NormalizeRegulation(int regulation)
        {
            return Enum.IsDefined(typeof(PitServiceRegulation), regulation)
                ? (PitServiceRegulation)regulation
                : PitServiceRegulation.DefaultSequential;
        }

        public static double NormalizeNecRefuelRatePercent(double percent)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent) || percent <= 0.0)
            {
                return DefaultNecRefuelRatePercent;
            }

            if (percent < MinNecRefuelRatePercent) return MinNecRefuelRatePercent;
            if (percent > MaxNecRefuelRatePercent) return MaxNecRefuelRatePercent;
            return percent;
        }

        public static PitServiceTimeResult Calculate(
            PitServiceRegulation regulation,
            double fuelLitres,
            double baseRefuelRateLps,
            double tyreSeconds,
            double serviceOverheadSeconds,
            double necRefuelRatePercent)
        {
            var normalizedRegulation = NormalizeRegulation(regulation);
            double safeFuelLitres = SanitizeNonNegative(fuelLitres);
            double safeBaseRate = SanitizeNonNegative(baseRefuelRateLps);
            double safeTyreSeconds = SanitizeNonNegative(tyreSeconds);
            double safeOverhead = SanitizeNonNegative(serviceOverheadSeconds);
            double safeNecPercent = NormalizeNecRefuelRatePercent(necRefuelRatePercent);

            double effectiveRate = safeBaseRate;
            if (normalizedRegulation == PitServiceRegulation.NecSimultaneous && safeBaseRate > 0.0)
            {
                effectiveRate = safeBaseRate * safeNecPercent / 100.0;
            }

            double fuelSeconds = (safeFuelLitres > 0.0 && effectiveRate > 0.0)
                ? safeFuelLitres / effectiveRate
                : 0.0;
            fuelSeconds = SanitizeNonNegative(fuelSeconds);

            double coreServiceSeconds = CalculateCoreServiceSeconds(normalizedRegulation, fuelSeconds, safeTyreSeconds);

            return new PitServiceTimeResult
            {
                Regulation = normalizedRegulation,
                FuelSeconds = fuelSeconds,
                TyreSeconds = safeTyreSeconds,
                CoreServiceSeconds = SanitizeNonNegative(coreServiceSeconds),
                ServiceSecondsWithOverhead = SanitizeNonNegative(coreServiceSeconds + safeOverhead),
                EffectiveRefuelRateLps = SanitizeNonNegative(effectiveRate),
                NecFactorPercentUsed = normalizedRegulation == PitServiceRegulation.NecSimultaneous
                    ? safeNecPercent
                    : PitServiceTimeModel.DefaultNecRefuelRatePercent
            };
        }


        public static PitServiceTimeResult CalculateFromFuelSeconds(
            PitServiceRegulation regulation,
            double fuelSeconds,
            double effectiveRefuelRateLps,
            double tyreSeconds,
            double serviceOverheadSeconds,
            double necRefuelRatePercent)
        {
            var normalizedRegulation = NormalizeRegulation(regulation);
            double safeFuelSeconds = SanitizeNonNegative(fuelSeconds);
            double safeTyreSeconds = SanitizeNonNegative(tyreSeconds);
            double safeOverhead = SanitizeNonNegative(serviceOverheadSeconds);
            double safeNecPercent = NormalizeNecRefuelRatePercent(necRefuelRatePercent);

            double coreServiceSeconds = CalculateCoreServiceSeconds(normalizedRegulation, safeFuelSeconds, safeTyreSeconds);

            return new PitServiceTimeResult
            {
                Regulation = normalizedRegulation,
                FuelSeconds = safeFuelSeconds,
                TyreSeconds = safeTyreSeconds,
                CoreServiceSeconds = SanitizeNonNegative(coreServiceSeconds),
                ServiceSecondsWithOverhead = SanitizeNonNegative(coreServiceSeconds + safeOverhead),
                EffectiveRefuelRateLps = SanitizeNonNegative(effectiveRefuelRateLps),
                NecFactorPercentUsed = normalizedRegulation == PitServiceRegulation.NecSimultaneous
                    ? safeNecPercent
                    : PitServiceTimeModel.DefaultNecRefuelRatePercent
            };
        }

        private static double CalculateCoreServiceSeconds(PitServiceRegulation regulation, double fuelSeconds, double tyreSeconds)
        {
            switch (regulation)
            {
                case PitServiceRegulation.ImsaSimultaneous:
                case PitServiceRegulation.NecSimultaneous:
                    return Math.Max(fuelSeconds, tyreSeconds);
                case PitServiceRegulation.DefaultSequential:
                default:
                    return fuelSeconds + tyreSeconds;
            }
        }

        private static double SanitizeNonNegative(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
            {
                return 0.0;
            }

            return value;
        }
    }

    public sealed class PitServiceRegulationJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            Type targetType = Nullable.GetUnderlyingType(objectType) ?? objectType;
            return targetType == typeof(PitServiceRegulation);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool nullable = Nullable.GetUnderlyingType(objectType) != null;
            if (reader.TokenType == JsonToken.Null)
            {
                return nullable ? null : (object)PitServiceRegulation.DefaultSequential;
            }

            try
            {
                if (reader.TokenType == JsonToken.Integer)
                {
                    int value = Convert.ToInt32(reader.Value);
                    return PitServiceTimeModel.NormalizeRegulation(value);
                }

                if (reader.TokenType == JsonToken.String)
                {
                    string text = (reader.Value as string ?? string.Empty).Trim();
                    if (int.TryParse(text, out int numeric))
                    {
                        return PitServiceTimeModel.NormalizeRegulation(numeric);
                    }

                    if (Enum.TryParse(text, ignoreCase: true, result: out PitServiceRegulation parsed))
                    {
                        return PitServiceTimeModel.NormalizeRegulation(parsed);
                    }
                }
            }
            catch
            {
                // Fall through to safe default; preset/profile compatibility is more important than strict parsing.
            }

            return PitServiceRegulation.DefaultSequential;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var regulation = value is PitServiceRegulation typed
                ? PitServiceTimeModel.NormalizeRegulation(typed)
                : PitServiceRegulation.DefaultSequential;
            writer.WriteValue((int)regulation);
        }
    }
}
