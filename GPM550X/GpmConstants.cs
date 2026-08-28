namespace Gpm
{
    internal static class GpmConstants
    {
        public const string MissileJsonKey = "missilepack_gpm550x";
        public const string MountKeyPrefix = "MissilePack_GPM550X_";
        public const string MountJsonKeySingle = "MissilePack_GPM550X_single";
        public const string MountJsonKeyGpo = "MissilePack_GPM550X_gpoSingle";
        public const string WeaponInfoName = "GPM-550X";
        public const string MountDisplayName = "GPM-550X";
        public const string UnitName = "GPM-550X";
        public const string ShortName = "GPM-550X";
        public const string BogeyName = "GPM-550X";
        public const string SeekerTypeName = "INS / Opt.";
        public const string Description =
            "Quasi-ballistic extended-range strike missile. INS/optical, 550kg HE, 100km from rest.";

        public const string VisualRootName = "GpmVisual";
        public const string MeshPrefabAsset = VisualRootName;
        public const string BundleModName = "GPM550X";
        public const string NobpFileName = "GPM550X.nobp";
        public const string PreviewIconFileName = "PreviewGpm.png";
        public const string PreviewIconResource = "Gpm.Resources.PreviewGpm.png";
        public const int PreviewIconAlphaBase = 255;
        public const int PreviewIconDarkLuma = 145;

        public const string ShellMissileKey = "AShM3";
        public const string ShellMissileKeyAlt = "AShM3_single";
        public const string TuskoPrefix = "AShM3";
        public const string Ashm300SlotPrefix = "AShM1";
        public const string GpoPrefix = "bomb_500";
        public const string GpoWeaponName = "GPO-500";
        public const string TuskoMountSingle = "AShM3_single";

        public const float LaunchMassKg = 877.5f;
        public const float BlastYieldKg = 550f;
        public const float Cost = 2.8f;
        // ~3.9 m body; non-VLO quasi-ballistic (~0.18 m² vs stealth 0.07).
        public const float RadarSize = 0.3f;
        public const float LengthM = 3.9f;
        public const float WidthM = 0.6f;
        public const float HeightM = 0.6f;
        public const float VisualScaleMult = 1f;
        public const float MountClearanceM = 0.02f;
        public const float PylonLiftExtraM = 0.30f;
        public const float RailStationHalfM = 0.45f;
        public const float MountEmptyMassKg = 20f;
        public const float FbxChildScale = 100f;

        public const float DesignRangeM = 100000f;
        public const float EncyclopediaMinRangeM = 2000f;
        public const float CalcRestLaunchSpeedMps = 0f;
        public const float CalcRestLaunchAltM = 0f;
        public const float CalcRestTargetAltM = 0f;
        public const float CalcRestTargetDistM = 100000f;
        public const float Pk = 0.65f;

        public const float DonorWikiMassKg = 650f;
        public const float DonorWikiRangeM = 60000f;

        // 37 kN / 27 s sustain; 3.1M cap; sea-level CalcRange ~100 km.
        public const float MotorThrustN = 37000f;
        public const float MotorFuelKg = 125f;
        public const float MotorBurnS = 27f;
        public const float DesignTopSpeedMach = 3.1f;
        public const float SeaLevelSpeedOfSoundMps = 340f;
        public const float DesignTopSpeedMps = DesignTopSpeedMach * SeaLevelSpeedOfSoundMps;
        public const float CruiseAltitudeM = 10000f;
        public const float SeekerGLimit = 5f;
        public const float UprightPreference = 0.5f;
        public const float BankKillRate = 0.28f;
        public const float FxWorldScaleM = 1f;
        public const float FxAftNudgeM = 0.08f;

        public static readonly string[] AttachPylonAliases =
        {
            "DockingPlace", "PlaceOfRocketLock", "Attach_Pylon", "Pylon", "Mount", "Hardpoint"
        };

        public static readonly string[] DockAliases =
        {
            "DockingPort"
        };

        public static readonly string[] EngineAliases =
        {
            "EngineEffectsSpawn", "EngineEffects", "PlaceOfSpawnEngineEffectsAndLight",
            "PlaceOfEngine", "Exhaust", "Nozzle"
        };

        public static readonly string[] AftAliases =
        {
            "EngineEffectsSpawn", "EngineEffects", "AerodynamicStabilizator"
        };

        public static readonly string[] NoseAliases =
        {
            "FlightController", "Main"
        };

        public static readonly string[] ForeignOwnerTags =
        {
            "WarewindTag", "CrosswimTag", "Mk54Tag", "HydraTag", "TorpedoTag"
        };
    }
}
