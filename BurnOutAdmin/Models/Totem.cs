using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.Models;

/// <summary>
/// <para>
/// Représente un totem (identité animale) qu'un coach peut attribuer à un
/// client. Le <see cref="Rank"/> correspond à <c>clients.totem_rang</c> en base
/// (index 0..60 dans la liste de référence — l'ordre fait foi côté app mobile
/// dans <c>lib/models/totem.dart</c>).
/// </para>
///
/// <para><b>Rang vs. badge.</b> <see cref="Rank"/> identifie uniquement
/// l'animal. Le badge réellement affiché côté UI est composé de
/// <c>(tier × division × slug)</c> ; tier et division sont calculés depuis le
/// total de PE du client (<c>client_stats.points</c>) via
/// <see cref="RankCalculator"/>, l'URL Cloudinary est résolue par le service
/// de badges puis normalisée par <see cref="BadgeUrlBuilder"/>.</para>
///
/// <para><b>Note importante</b> : la liste <see cref="All"/> ci-dessous ne
/// contient que les 8 premiers totems pour lesquels les badges existent
/// effectivement en base (<c>rank_badges</c>). Quand le pipeline media
/// téléversera les autres animaux, il suffira d'étendre cette liste — l'ordre
/// doit rester strictement aligné avec celui de l'app mobile.</para>
/// </summary>
public class Totem
{
    /// <summary>Index en base (clients.totem_rang). Stable et permanent.</summary>
    public int Rank { get; init; }
    /// <summary>Slug technique utilisé dans rank_badges.totem_slug.</summary>
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Emoji { get; init; } = string.Empty;
    public Color AccentColor { get; init; } = Color.FromArgb("#4F46E5");
    public Color AccentBackground { get; init; } = Color.FromArgb("#EEF2FF");

    /// <summary>
    /// URL Cloudinary à afficher pour ce totem dans des contextes où on n'a
    /// pas (encore) calculé le tier+division du client (ex. picker de
    /// sélection). On utilise le palier le plus prestigieux disponible
    /// (<c>or/3</c>) comme aperçu — déjà normalisé via
    /// <see cref="BadgeUrlBuilder"/>.
    /// </summary>
    public string ImageUrl => BadgeUrlBuilder.Normalize(
        $"https://res.cloudinary.com/dnb9pqsca/image/upload/ranks/or/3/{Slug}.png");

    /// <summary>
    /// Catalogue des totems disponibles. <b>L'ordre doit rester aligné avec
    /// <c>kTotems</c> côté app mobile</b> — c'est lui qui fait foi pour
    /// l'index stocké en base.
    /// </summary>
    public static IReadOnlyList<Totem> All { get; } = new[]
    {
        new Totem
        {
            Rank             = 0,
            Slug             = "coccinelle",
            DisplayName      = "Coccinelle",
            Emoji            = "🐞",
            AccentColor      = Color.FromArgb("#DC2626"),
            AccentBackground = Color.FromArgb("#FEF2F2")
        },
        new Totem
        {
            Rank             = 1,
            Slug             = "lapin",
            DisplayName      = "Lapin",
            Emoji            = "🐰",
            AccentColor      = Color.FromArgb("#7C3AED"),
            AccentBackground = Color.FromArgb("#F5F3FF")
        },
        new Totem
        {
            Rank             = 2,
            Slug             = "lezard",
            DisplayName      = "Lézard",
            Emoji            = "🦎",
            AccentColor      = Color.FromArgb("#16A34A"),
            AccentBackground = Color.FromArgb("#F0FDF4")
        },
        new Totem
        {
            Rank             = 3,
            Slug             = "abeille",
            DisplayName      = "Abeille",
            Emoji            = "🐝",
            AccentColor      = Color.FromArgb("#EA580C"),
            AccentBackground = Color.FromArgb("#FFF7ED")
        },
        new Totem
        {
            Rank             = 4,
            Slug             = "panda",
            DisplayName      = "Panda",
            Emoji            = "🐼",
            AccentColor      = Color.FromArgb("#0F172A"),
            AccentBackground = Color.FromArgb("#F1F5F9")
        },
        new Totem
        {
            Rank             = 5,
            Slug             = "chat",
            DisplayName      = "Chat",
            Emoji            = "🐱",
            AccentColor      = Color.FromArgb("#0891B2"),
            AccentBackground = Color.FromArgb("#ECFEFF")
        },
        new Totem
        {
            Rank             = 6,
            Slug             = "chien",
            DisplayName      = "Chien",
            Emoji            = "🐶",
            AccentColor      = Color.FromArgb("#B45309"),
            AccentBackground = Color.FromArgb("#FEFCE8")
        },
        new Totem
        {
            Rank             = 7,
            Slug             = "ouistiti",
            DisplayName      = "Ouistiti",
            Emoji            = "🐵",
            AccentColor      = Color.FromArgb("#D97706"),
            AccentBackground = Color.FromArgb("#FFFBEB")
        }
    };

    /// <summary>Récupère le totem correspondant à <paramref name="rank"/> ou <c>null</c>.</summary>
    public static Totem? ByRank(int? rank)
        => rank is { } r ? All.FirstOrDefault(t => t.Rank == r) : null;
}
