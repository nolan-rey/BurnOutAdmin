using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

/// <summary>
/// Résout l'URL d'un badge à partir de sa clé composée
/// <c>(tier × division × totem_slug)</c>. Charge en mémoire la table
/// <c>rank_badges</c> (~224 lignes) à la première utilisation pour éviter
/// un round-trip par badge affiché.
/// </summary>
public interface IRankBadgeService
{
    /// <summary>
    /// Retourne l'URL Cloudinary <b>normalisée</b> du badge ou <c>null</c>
    /// si aucune ligne ne correspond (UI doit alors basculer en fallback).
    /// </summary>
    Task<string?> GetBadgeUrlAsync(ClientRank rank, string totemSlug);
}
