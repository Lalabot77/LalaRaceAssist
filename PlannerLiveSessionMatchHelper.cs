using System;

namespace LaunchPlugin
{
    internal sealed class PlannerLiveSessionMatchSnapshot
    {
        public string LiveCar = string.Empty;
        public string LiveTrack = string.Empty;
        public bool HasLiveBasis;
        public bool LiveBasisIsTimeLimited;
        public bool HasLiveRaceLength;
        public double LiveRaceLengthValue;

        public string PlannerCar = string.Empty;
        public string PlannerTrack = string.Empty;
        public bool HasPlannerBasis;
        public bool PlannerBasisIsTimeLimited;
        public bool HasPlannerRaceLength;
        public double PlannerRaceLengthValue;
    }

    internal sealed class PlannerLiveSessionMatchResult
    {
        public bool IsMatch;
        public bool CarMatch;
        public bool TrackMatch;
        public bool BasisMatch;
        public bool RaceLengthMatch;
        public bool HasComparableInputs;
    }

    internal static class PlannerLiveSessionMatchHelper
    {
        public const double TimeRaceLengthToleranceMinutes = 0.10;
        public const double LapRaceLengthToleranceLaps = 0.01;

        public static PlannerLiveSessionMatchResult Evaluate(PlannerLiveSessionMatchSnapshot snapshot)
        {
            var result = new PlannerLiveSessionMatchResult();
            if (snapshot == null)
            {
                return result;
            }

            string liveCar = (snapshot.LiveCar ?? string.Empty).Trim();
            string plannerCar = (snapshot.PlannerCar ?? string.Empty).Trim();
            string liveTrack = (snapshot.LiveTrack ?? string.Empty).Trim();
            string plannerTrack = (snapshot.PlannerTrack ?? string.Empty).Trim();

            bool hasCars = !string.IsNullOrWhiteSpace(liveCar) && !string.IsNullOrWhiteSpace(plannerCar);
            bool hasTracks = !string.IsNullOrWhiteSpace(liveTrack) && !string.IsNullOrWhiteSpace(plannerTrack);
            bool hasBasis = snapshot.HasLiveBasis && snapshot.HasPlannerBasis;
            bool hasRaceLength =
                snapshot.HasLiveRaceLength &&
                snapshot.HasPlannerRaceLength &&
                snapshot.LiveRaceLengthValue > 0.0 &&
                snapshot.PlannerRaceLengthValue > 0.0;

            result.CarMatch = hasCars && string.Equals(liveCar, plannerCar, StringComparison.OrdinalIgnoreCase);
            result.TrackMatch = hasTracks && string.Equals(liveTrack, plannerTrack, StringComparison.OrdinalIgnoreCase);
            result.BasisMatch = hasBasis && (snapshot.LiveBasisIsTimeLimited == snapshot.PlannerBasisIsTimeLimited);

            if (result.BasisMatch && hasRaceLength)
            {
                double tolerance = snapshot.LiveBasisIsTimeLimited
                    ? TimeRaceLengthToleranceMinutes
                    : LapRaceLengthToleranceLaps;
                result.RaceLengthMatch = Math.Abs(snapshot.LiveRaceLengthValue - snapshot.PlannerRaceLengthValue) <= tolerance;
            }
            else
            {
                result.RaceLengthMatch = false;
            }

            result.HasComparableInputs = hasCars && hasTracks && hasBasis && hasRaceLength;
            result.IsMatch = result.CarMatch && result.TrackMatch && result.BasisMatch && result.RaceLengthMatch;
            return result;
        }
    }
}
