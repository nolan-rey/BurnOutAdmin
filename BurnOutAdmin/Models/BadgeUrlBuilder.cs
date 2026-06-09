namespace BurnOutAdmin.Models;

/// <summary>
/// Utilitaire de normalisation des URLs Cloudinary des badges.
///
/// Les images sont uploadées brutes (marges variables), il faut donc insérer
/// une transformation Cloudinary <b>juste après</b> <c>/image/upload/</c> pour
/// que toutes les images apparaissent à la même taille avec fond transparent :
///
/// <code>e_trim/c_pad,b_transparent,w_200,h_200/</code>
///
/// <para>Exemple :</para>
/// <code>
/// brut :
///   https://res.cloudinary.com/dnb9pqsca/image/upload/v123/ranks/argent/2/panda.png
///
/// normalisé :
///   https://res.cloudinary.com/dnb9pqsca/image/upload/e_trim/c_pad,b_transparent,w_200,h_200/v123/ranks/argent/2/panda.png
/// </code>
///
/// <para>Cette transformation est idempotente : si elle est déjà présente
/// dans l'URL, l'appel ne modifie rien.</para>
/// </summary>
public static class BadgeUrlBuilder
{
    private const string UploadMarker = "/image/upload/";
    private const string Transform    = "e_trim/c_pad,b_transparent,w_200,h_200/";

    /// <summary>
    /// Insère la transformation Cloudinary dans l'URL si elle n'y est pas
    /// déjà. Retourne la chaîne telle quelle si l'URL ne contient pas
    /// <c>/image/upload/</c> (cas d'une URL non Cloudinary ou d'un fallback).
    /// </summary>
    public static string Normalize(string? url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        var markerIndex = url.IndexOf(UploadMarker, StringComparison.Ordinal);
        if (markerIndex < 0) return url;

        var insertAt = markerIndex + UploadMarker.Length;
        // Idempotent : si la transform est déjà juste après le marker, no-op.
        if (url.AsSpan(insertAt).StartsWith(Transform.AsSpan()))
            return url;

        return string.Concat(
            url.AsSpan(0, insertAt),
            Transform.AsSpan(),
            url.AsSpan(insertAt));
    }

    /// <summary>
    /// Construit une URL Cloudinary normalisée pour un badge à partir des
    /// composantes <c>(tier, division, slug)</c>. Diamant utilise la
    /// convention division=0 conformément à la base.
    /// </summary>
    public static string BuildBadgeUrl(string tier, int division, string slug)
        => Normalize($"https://res.cloudinary.com/dnb9pqsca/image/upload/ranks/{tier}/{division}/{slug}.png");
}
