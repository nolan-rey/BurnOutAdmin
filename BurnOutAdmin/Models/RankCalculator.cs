namespace BurnOutAdmin.Models;

/// <summary>
/// Tier × division composant le badge d'un client. Diamant est un palier
/// unique → division 0 par convention (pas de I/II/III à afficher).
/// </summary>
public record struct ClientRank(string Tier, int Division)
{
    public static ClientRank Default => new("carbone", 1);

    /// <summary>Vrai si le tier est Diamant (palier unique sans chevrons).</summary>
    public bool IsDiamant => string.Equals(Tier, "diamant", StringComparison.OrdinalIgnoreCase);

    /// <summary>Libellé court FR pour affichage (ex. « Argent II », « Diamant »).</summary>
    public string DisplayLabel
    {
        get
        {
            var pretty = Tier.Length == 0
                ? Tier
                : char.ToUpperInvariant(Tier[0]) + Tier[1..];
            return IsDiamant
                ? pretty
                : $"{pretty} {Division switch { 1 => "I", 2 => "II", 3 => "III", _ => Division.ToString() }}";
        }
    }
}

/// <summary>
/// Calculateur de palier (tier × division) à partir du total de PE
/// (<c>client_stats.points</c>). Algorithme : on parcourt la table de paliers
/// triée par PE croissants et on retient le <b>dernier</b> palier dont
/// <c>PeMin ≤ points</c>. Diamant est le palier maximum, division 0.
///
/// <para>Table de paliers documentée dans
/// <c>docs/admin_totem_badge.md</c> et alignée 1-1 avec celle de l'app mobile.</para>
/// </summary>
public static class RankCalculator
{
    /// <summary>Un palier (tier × division → PE minimum requis).</summary>
    public record struct RankPalier(string Tier, int Division, int PeMin);

    /// <summary>
    /// Table immuable des 28 paliers. <b>Ne pas réordonner</b> — l'algorithme
    /// suppose un ordre croissant strict sur <c>PeMin</c>.
    /// </summary>
    public static IReadOnlyList<RankPalier> Paliers { get; } = new RankPalier[]
    {
        new("carbone",  1,     0),
        new("carbone",  2,   100),
        new("carbone",  3,   200),
        new("fer",      1,   300),
        new("fer",      2,   500),
        new("fer",      3,   700),
        new("bronze",   1,   900),
        new("bronze",   2,  1200),
        new("bronze",   3,  1500),
        new("argent",   1,  1800),
        new("argent",   2,  2200),
        new("argent",   3,  2600),
        new("or",       1,  3000),
        new("or",       2,  3500),
        new("or",       3,  4000),
        new("platine",  1,  4500),
        new("platine",  2,  5100),
        new("platine",  3,  5700),
        new("saphir",   1,  6300),
        new("saphir",   2,  7000),
        new("saphir",   3,  7700),
        new("rubis",    1,  8400),
        new("rubis",    2,  9200),
        new("rubis",    3, 10000),
        new("emeraude", 1, 10800),
        new("emeraude", 2, 11700),
        new("emeraude", 3, 12600),
        new("diamant",  0, 13500)
    };

    /// <summary>
    /// Retourne le palier (tier × division) correspondant au total de PE
    /// passé en paramètre. Renvoie <c>carbone/1</c> par défaut si la valeur
    /// est négative ou nulle (compte juste créé).
    /// </summary>
    public static ClientRank ForPoints(int points)
    {
        if (points <= 0) return ClientRank.Default;

        // On parcourt en ordre croissant : on retient le dernier palier
        // dont PeMin <= points. Sortie anticipée dès qu'on dépasse.
        var current = Paliers[0];
        foreach (var palier in Paliers)
        {
            if (points >= palier.PeMin) current = palier;
            else break;
        }
        return new ClientRank(current.Tier, current.Division);
    }
}
