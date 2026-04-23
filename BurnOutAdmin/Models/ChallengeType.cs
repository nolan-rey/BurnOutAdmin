namespace BurnOutAdmin.Models;

public enum ChallengeType
{
    General,
    Cardio,
    Force,
    Endurance,
    Poids,
    Flexibilite
}

public static class ChallengeTypeExtensions
{
    public static string ToFrenchString(this ChallengeType type) => type switch
    {
        ChallengeType.Cardio      => "Cardio",
        ChallengeType.Force       => "Force",
        ChallengeType.Endurance   => "Endurance",
        ChallengeType.Poids       => "Poids",
        ChallengeType.Flexibilite => "Flexibilité",
        _                         => "Général"
    };

    public static string ToIcon(this ChallengeType type) => type switch
    {
        ChallengeType.Cardio      => "\uE566", // directions_run
        ChallengeType.Force       => "\uE3A9", // fitness_center (dumbbell)
        ChallengeType.Endurance   => "\uE52F", // directions_bike
        ChallengeType.Poids       => "\uEA26", // balance / scale
        ChallengeType.Flexibilite => "\uEA78", // self_improvement
        _                         => "\uEBD2"  // emoji_events (trophy)
    };
}
