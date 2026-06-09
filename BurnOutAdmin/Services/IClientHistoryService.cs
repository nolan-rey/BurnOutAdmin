using BurnOutAdmin.Models;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.Services;

/// <summary>
/// Historique d'activité d'un client : ses séances assignées (à faire +
/// réalisées) et, si présentes, ses performances par exercice.
/// </summary>
public interface IClientHistoryService
{
    /// <summary>
    /// Récupère toutes les séances assignées à un client (réalisées et à venir),
    /// triées de la plus récente à la plus ancienne.
    /// </summary>
    Task<List<ClientSeanceItem>> GetClientSeancesAsync(int clientId);

    /// <summary>
    /// Charge les performances réelles saisies par le client pour une séance
    /// donnée (table <c>performance_client</c>, jointe via les copies de la
    /// séance dans les programmes). Liste vide si aucune performance saisie.
    /// </summary>
    Task<List<ClientPerformanceItem>> GetPerformancesForSessionAsync(int clientId, int builderId);

    /// <summary>
    /// Récupère le feedback de fin de séance saisi par le client : RPE global,
    /// RPE détaillé par catégorie, humeur, durée, commentaires, photo.
    ///
    /// Source primaire : table <c>seance_rpe</c> reliée à l'assignation par
    /// <c>id_assignation</c> (UUID) — donne un lien 1:1 fiable, indépendant
    /// d'une publication sociale.
    ///
    /// Fallback (compatibilité historique) : table <c>posts</c> matchée par
    /// client + fenêtre temporelle (algorithme du plus proche voisin mutuel)
    /// si aucune ligne <c>seance_rpe</c> n'existe pour cette assignation.
    /// Retourne <c>null</c> si rien n'est trouvé.
    /// </summary>
    Task<ClientSessionFeedback?> GetSessionFeedbackAsync(
        int clientId,
        DateTime dateRealisation,
        string? idAssignation = null);
}

/// <summary>Performance saisie par un client pour un exercice donné.</summary>
public class ClientPerformanceItem
{
    public int    IdContenu       { get; set; }
    public string ExerciseName    { get; set; } = string.Empty;
    public int?   SeriesFaites    { get; set; }
    public string? RepetitionsFaites { get; set; }
    public double? ChargeFaiteKg  { get; set; }
    public int?    Ressenti       { get; set; }
}

/// <summary>
/// Feedback de fin de séance saisi par le client dans son app (table <c>posts</c>).
/// Toutes les valeurs sont optionnelles.
/// </summary>
public class ClientSessionFeedback
{
    public int      IdPost          { get; set; }
    public int?     Rpe             { get; set; }       // 0-10 (legacy)
    public double?  RpeGeneral      { get; set; }       // 0-10 avec décimale (plus précis)
    public string?  Mood            { get; set; }       // emoji
    public int?     DurationMin     { get; set; }
    public int?     DurationSec     { get; set; }       // précision en secondes (seance_rpe)
    public int?     ExosValidated   { get; set; }       // nb d'exercices validés par le client
    public int?     ExosSkipped     { get; set; }       // nb d'exercices sautés
    public string?  Content         { get; set; }       // commentaire libre (posts uniquement)
    public string?  ImageUrl        { get; set; }       // photo de fin de séance (posts uniquement)
    public DateTime CreatedAt       { get; set; }

    /// <summary>RPE détaillé (3 catégories × items + commentaires) si saisi.</summary>
    public RpeDetailedFeedback? RpeDetails { get; set; }

    public bool HasContent      => !string.IsNullOrWhiteSpace(Content);
    public bool HasMood         => !string.IsNullOrWhiteSpace(Mood);
    public bool HasImage        => !string.IsNullOrWhiteSpace(ImageUrl);
    public bool HasRpe          => RpeGeneral is > 0 || Rpe is > 0;
    public bool HasDuration     => (DurationMin is > 0) || (DurationSec is > 0);
    public bool HasRpeDetails   => RpeDetails is { HasAnyContent: true };
    public bool HasExosBreakdown => (ExosValidated ?? 0) + (ExosSkipped ?? 0) > 0;

    public string RpeDisplay      => RpeGeneral is { } g ? $"{g:0.#}/10"
                                   : Rpe is { } r ? $"{r}/10"
                                   : "—";

    /// <summary>Durée formatée intelligemment selon la précision disponible.</summary>
    public string DurationDisplay
    {
        get
        {
            // Précision seconde si on en dispose et que c'est court.
            if (DurationSec is { } s && s > 0)
            {
                if (s < 60)               return $"{s} s";
                if (s < 3600)             return $"{s / 60} min {s % 60:D2}";
                return $"{s / 3600} h {(s % 3600) / 60:D2}";
            }
            if (DurationMin is { } m && m > 0)
            {
                if (m < 60) return $"{m} min";
                return $"{m / 60} h {m % 60:D2}";
            }
            return "—";
        }
    }

    public string ExosDisplay
    {
        get
        {
            var v = ExosValidated ?? 0;
            var k = ExosSkipped   ?? 0;
            return k > 0 ? $"{v} ✓ · {k} sautés" : $"{v} ✓";
        }
    }

    public string CreatedDisplay  => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy 'à' HH:mm");
}

/// <summary>
/// Détail complet du RPE saisi par le client en 3 catégories
/// (Spécifique, Connexe, Annexe). Respecte la visibilité (<c>hidden</c>)
/// choisie par le client : un item masqué n'est pas affiché.
/// </summary>
public class RpeDetailedFeedback
{
    public double? General { get; set; }

    public RpeCategoryView Specifique { get; set; } =
        new("Spécifique", "Effort propre à la séance");
    public RpeCategoryView Connexe    { get; set; } =
        new("Connexe", "État physiologique du jour");
    public RpeCategoryView Annexe     { get; set; } =
        new("Annexe", "Contexte de vie extérieur");

    public bool HasAnyContent =>
        Specifique.HasAnyContent || Connexe.HasAnyContent || Annexe.HasAnyContent;

    public IEnumerable<RpeCategoryView> Categories
    {
        get
        {
            if (Specifique.HasAnyContent) yield return Specifique;
            if (Connexe.HasAnyContent)    yield return Connexe;
            if (Annexe.HasAnyContent)     yield return Annexe;
        }
    }
}

/// <summary>Vue d'une catégorie de RPE détaillé (titre + items + commentaire).</summary>
public class RpeCategoryView
{
    public RpeCategoryView(string title, string subtitle)
    {
        Title    = title;
        Subtitle = subtitle;
    }

    public string Title    { get; }
    public string Subtitle { get; }
    public double? AverageScore { get; set; }
    public string? Comment      { get; set; }
    public List<RpeItemView> Items { get; set; } = new();

    public bool HasComment => !string.IsNullOrWhiteSpace(Comment);
    public bool HasItems   => Items.Count > 0;
    public bool HasAnyContent => HasItems || HasComment;
    public string AverageDisplay => AverageScore is { } a ? $"{a:0.#}/10" : "—";

    /// <summary>Couleur de fond du badge moyenne (vert / ambre / rouge selon intensité).</summary>
    public Color AverageBg => AverageScore switch
    {
        <= 3.5 => Color.FromArgb("#F0FDF4"),   // facile / léger
        <= 6.5 => Color.FromArgb("#FFFBEB"),   // modéré
        _      => Color.FromArgb("#FEF2F2")    // dur
    };

    public Color AverageColor => AverageScore switch
    {
        <= 3.5 => Color.FromArgb("#15803D"),
        <= 6.5 => Color.FromArgb("#B45309"),
        _      => Color.FromArgb("#B91C1C")
    };
}

/// <summary>Un item de RPE : nom lisible + score 0-10.</summary>
public class RpeItemView
{
    public string Label { get; set; } = string.Empty;
    public double Score { get; set; }
    public string ScoreDisplay => $"{Score:0.#}";

    /// <summary>Largeur de la barre de progression (0-100%) pour l'affichage.</summary>
    public double ScorePercent => Math.Clamp(Score * 10, 0, 100);

    /// <summary>Progression normalisée (0-1) pour <see cref="ProgressBar"/>.</summary>
    public double Progress01 => Math.Clamp(Score / 10.0, 0, 1);

    /// <summary>Couleur de la barre selon le score.</summary>
    public Color BarColor => Score switch
    {
        <= 3.5 => Color.FromArgb("#22C55E"),
        <= 6.5 => Color.FromArgb("#F59E0B"),
        _      => Color.FromArgb("#EF4444")
    };
}
