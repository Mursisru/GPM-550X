using System;

namespace Gpm.Bootstrap
{
    /// <summary>GPO-500 single mount only on Vortex / Ifrit / Revoker. AShM-300 slot inject unchanged.</summary>
    internal static class GpmAircraftFilter
    {
        private static readonly string[] GpoMountJsonKeys =
        {
            "SmallFighter1", // FS-20 Vortex
            "Multirole1",    // KR-67 Ifrit
            "Revoker",       // FS-12 Revoker (current key)
            "Fighter1"       // legacy Revoker (V2LoadoutMap)
        };

        private static readonly string[] GpoNameTokens =
        {
            "Vortex",
            "Ifrit",
            "Revoker"
        };

        internal static bool AllowsGpoMount(AircraftDefinition? ad)
        {
            if (ad == null)
                return false;

            string? key = ad.jsonKey;
            if (!string.IsNullOrEmpty(key))
            {
                for (int i = 0; i < GpoMountJsonKeys.Length; i++)
                {
                    if (string.Equals(key, GpoMountJsonKeys[i], StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            if (MatchesNameToken(ad.unitName) || MatchesNameToken(ad.bogeyName) || MatchesNameToken(ad.code))
                return true;

            return false;
        }

        private static bool MatchesNameToken(string? text)
        {
            if (text is not { Length: > 0 })
                return false;
            for (int i = 0; i < GpoNameTokens.Length; i++)
            {
                string token = GpoNameTokens[i];
                if (string.IsNullOrEmpty(token))
                    continue;
                if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}
