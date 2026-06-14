using BurnOutAdmin.Models.Program;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Transforme un <see cref="SessionModel"/> .NET (PascalCase + structure
/// Categories → SubCategories → Exercises) vers le format JSON attendu par
/// l'API mobile (snake_case + structure plate <c>blocks → exercices</c>).
///
/// L'app mobile lit <c>data_json</c> et y cherche des clés précises :
///   <c>blocks[*].nom, partie_label, ordre, series, recup_secondes,
///   is_amrap, amrap_minutes, is_afap, afap_minutes, exercices[*]</c>
/// et pour chaque exercice :
///   <c>nom_exercice, series, reps, reps_varied, tempo, amplitude, rir,
///   rpe, recup_secondes, recup_inter_secondes, poids_label,
///   duree_secondes, set_details, image_url, url_video</c>.
///
/// Sans ce mapping, l'API recevait du PascalCase
/// (<c>Name, Categories, SubCategories…</c>) et la séance s'affichait vide
/// côté pratiquant.
/// </summary>
public static class MobileDataJsonMapper
{
    /// <summary>
    /// Construit l'objet à envoyer dans le champ <c>data_json</c> du body
    /// d'une requête API. Le retour est un <see cref="object"/> anonyme —
    /// PAS une chaîne — afin d'éviter le double encodage JSON que l'API
    /// ne sait pas re-décoder.
    /// </summary>
    public static object Build(SessionModel session)
    {
        var blocks = new List<object>();

        int catIdx = 0;
        foreach (var cat in session.Categories)
        {
            catIdx++;
            int subIdx = 0;
            foreach (var sub in cat.SubCategories)
            {
                subIdx++;
                blocks.Add(BuildBlock(cat, sub, catIdx, subIdx));
            }
        }

        return new
        {
            name        = session.Name,
            description = string.Empty,
            blocks      = blocks
        };
    }

    // ── Bloc (SubCategory) ────────────────────────────────────────────

    private static object BuildBlock(
        CategoryModel cat,
        SubCategoryModel sub,
        int catIdx,
        int subIdx)
    {
        // Partie label "1.A", "1.B", "2.A", etc. — convention historique
        var partieLabel = $"{catIdx}.{(char)('A' + System.Math.Min(subIdx - 1, 25))}";

        // AMRAP / AFAP : déduit du type de sous-cat (école n'a pas
        // encore les booléens explicites côté Models, fallback sur l'enum)
        var isAmrap = sub.Type == SubCategoryType.AMRAP;
        var isAfap  = false;

        var exercices = new List<object>();
        int exoOrder = 0;
        foreach (var ex in sub.Exercises)
        {
            exoOrder++;
            exercices.Add(BuildExercice(sub, ex, exoOrder));
        }

        return new
        {
            nom                  = string.IsNullOrWhiteSpace(sub.Name) ? cat.Name : sub.Name,
            partie_label         = partieLabel,
            ordre                = sub.Order > 0 ? sub.Order : subIdx,
            series               = sub.Sets > 0 ? sub.Sets : 1,
            recup_secondes       = sub.RestTime,
            note                 = sub.Note ?? string.Empty,
            is_amrap             = isAmrap,
            amrap_minutes        = 0,
            is_afap              = isAfap,
            afap_minutes         = 0,
            exercices            = exercices
        };
    }

    // ── Exercice ──────────────────────────────────────────────────────

    private static object BuildExercice(SubCategoryModel sub, ExerciseModel ex, int ordre)
    {
        // Reps / durée
        int? reps = null;
        int? dureeSec = null;
        string? repsVaried = null;

        if (ex.IsTimeMode)
        {
            dureeSec = ParseDureeSecondes(ex.RepsText);
        }
        else
        {
            reps = ParseInt(ex.RepsText);
        }

        // Set details : non géré sur la version école (pas de UsePerSetDetails dans
        // ExerciseModel école — les séries différenciées sont une feature client.)
        object? setDetails = null;

        return new
        {
            nom_exercice         = ex.Name,
            id_exercice          = 0, // école n'a pas de référence library
            ordre                = ordre,
            series               = sub.Sets > 0 ? sub.Sets : 1,
            reps                 = reps,
            reps_varied          = repsVaried,
            duree_secondes       = dureeSec,
            poids_label          = string.IsNullOrWhiteSpace(ex.WeightText) ? null : ex.WeightText.Trim(),
            recup_secondes       = sub.RestTime,
            recup_inter_secondes = ParseSecondes(ex.RecupInter) ?? 0,
            tempo                = string.IsNullOrWhiteSpace(ex.Tempo)     ? null : ex.Tempo.Trim(),
            amplitude            = string.IsNullOrWhiteSpace(ex.Amplitude) ? null : ex.Amplitude.Trim(),
            rir                  = ParseInt(ex.RirText),
            rpe                  = ex.Rpe > 0 ? (int?)ex.Rpe : null,
            image_url            = (string?)null,
            url_video            = (string?)null,
            description          = (string?)null,
            set_details          = setDetails
        };
    }

    // ── Helpers de parsing ────────────────────────────────────────────

    /// <summary>"30''" / "1'30" / "90" → 90 secondes.</summary>
    private static int? ParseSecondes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim().Replace("''", "").Replace("’’", "").Replace("\"", "");

        if (text.Contains('\'') || text.Contains('’'))
        {
            var parts = text.Replace('’', '\'').Split('\'',
                System.StringSplitOptions.RemoveEmptyEntries);
            int minutes = 0, seconds = 0;
            if (parts.Length >= 1) int.TryParse(parts[0], out minutes);
            if (parts.Length >= 2) int.TryParse(parts[1], out seconds);
            return minutes * 60 + seconds;
        }

        return int.TryParse(text, out var v) ? v : null;
    }

    /// <summary>"30s" / "1m30" / "90" → 90 secondes.</summary>
    private static int? ParseDureeSecondes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim().ToLowerInvariant().Replace(" ", "");

        if (text.Contains('m'))
        {
            var parts = text.Replace('s', ' ').Split('m',
                System.StringSplitOptions.RemoveEmptyEntries);
            int minutes = 0, seconds = 0;
            if (parts.Length >= 1) int.TryParse(parts[0], out minutes);
            if (parts.Length >= 2) int.TryParse(parts[1].Trim(), out seconds);
            return minutes * 60 + seconds;
        }

        text = text.Replace("s", string.Empty);
        return int.TryParse(text, out var v) ? v : null;
    }

    /// <summary>"10" → 10, "1010" → 1010, "" → null.</summary>
    private static int? ParseInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return int.TryParse(text.Trim(), out var v) ? v : null;
    }
}
