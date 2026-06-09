using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.Models;

/// <summary>
/// Évènement affiché dans la timeline « Activité Récente » du Dashboard.
/// Agrège des données provenant de plusieurs tables (séances réalisées,
/// challenges rejoints, programmes assignés, nouveaux clients, abonnements)
/// dans une représentation unifiée prête à afficher.
/// </summary>
public class RecentActivity
{
    public RecentActivityType Type { get; set; }
    public string ClientName  { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }

    // ── Vue : libellé temporel intelligent ───────────────────────
    public string TimeDisplay
    {
        get
        {
            var localNow = DateTime.Now;
            var local    = TimestampUtc.Kind == DateTimeKind.Utc
                ? TimestampUtc.ToLocalTime()
                : DateTime.SpecifyKind(TimestampUtc, DateTimeKind.Utc).ToLocalTime();
            var diff = localNow - local;

            if (diff.TotalMinutes < 1) return "à l'instant";
            if (diff.TotalMinutes < 60) return $"il y a {(int)diff.TotalMinutes} min";
            if (local.Date == localNow.Date) return local.ToString("HH:mm");
            if (local.Date == localNow.Date.AddDays(-1)) return "Hier";
            if (diff.TotalDays < 7)
            {
                var fr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                var day = local.ToString("dddd", fr);
                return char.ToUpperInvariant(day[0]) + day[1..];
            }
            return local.ToString("dd/MM");
        }
    }

    // ── Vue : icône Material Icons (point de code) ──
    public string Icon => Type switch
    {
        RecentActivityType.SeanceCompleted     => "\uE876", // check
        RecentActivityType.ChallengeJoined     => "\uEA65", // emoji_events
        RecentActivityType.ProgrammeAssigned   => "\uE85D", // assignment
        RecentActivityType.NewClient           => "\uE7FE", // person_add
        RecentActivityType.SubscriptionCreated => "\uE8F8", // card_membership
        _                                       => "\uE887"  // help_outline
    };

    public Color IconBackground => Type switch
    {
        RecentActivityType.SeanceCompleted     => Color.FromArgb("#F0FDF4"), // vert clair
        RecentActivityType.ChallengeJoined     => Color.FromArgb("#FFF7ED"), // orange clair
        RecentActivityType.ProgrammeAssigned   => Color.FromArgb("#EEF2FF"), // indigo clair
        RecentActivityType.NewClient           => Color.FromArgb("#F5F3FF"), // violet clair
        RecentActivityType.SubscriptionCreated => Color.FromArgb("#ECFEFF"), // cyan clair
        _ => Color.FromArgb("#F1F5F9")
    };

    public Color IconColor => Type switch
    {
        RecentActivityType.SeanceCompleted     => Color.FromArgb("#16A34A"),
        RecentActivityType.ChallengeJoined     => Color.FromArgb("#EA580C"),
        RecentActivityType.ProgrammeAssigned   => Color.FromArgb("#4F46E5"),
        RecentActivityType.NewClient           => Color.FromArgb("#7C3AED"),
        RecentActivityType.SubscriptionCreated => Color.FromArgb("#0891B2"),
        _ => Color.FromArgb("#64748B")
    };
}

public enum RecentActivityType
{
    SeanceCompleted,
    ChallengeJoined,
    ProgrammeAssigned,
    NewClient,
    SubscriptionCreated
}
