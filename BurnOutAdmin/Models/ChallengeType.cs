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
        ChallengeType.Cardio      => "🏃",
        ChallengeType.Force       => "💪",
        ChallengeType.Endurance   => "🚴",
        ChallengeType.Poids       => "⚖️",
        ChallengeType.Flexibilite => "🧘",
        _                         => "🏆"
    };
}
