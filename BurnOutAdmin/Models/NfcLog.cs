using SQLite;

namespace BurnOutAdmin.Models;

/// <summary>
/// Représente un événement d'accès NFC (scan, bind, résultat).
/// Persisté en SQLite via sqlite-net-pcl.
/// </summary>
public class NfcLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Identifiant unique de l'événement (corrélation MQTT request/response).</summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>UID de la carte NFC scannée.</summary>
    public string Uid { get; set; } = string.Empty;

    /// <summary>Id du client associé (null si carte inconnue).</summary>
    public int? ClientId { get; set; }

    /// <summary>Nom complet du client (dénormalisé pour affichage rapide).</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>Résultat de l'accès.</summary>
    public NfcAccessResult Result { get; set; }

    /// <summary>Raison du résultat (ex: "Abonnement expiré", "Carte inconnue").</summary>
    public string? Reason { get; set; }

    /// <summary>Horodatage UTC de l'événement.</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Identifiant de la porte/lecteur (multi-points d'accès).</summary>
    public string? Door { get; set; }

    /// <summary>Source de l'événement (ex: "rfid-reader-01", "mqtt", "manual").</summary>
    public string Source { get; set; } = string.Empty;

    // --- Propriétés calculées (non persistées) ---

    [Ignore]
    public string TimestampFormatted => TimestampUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");

    [Ignore]
    public string ResultText => Result switch
    {
        NfcAccessResult.Authorized => "Autorisé",
        NfcAccessResult.Denied => "Refusé",
        NfcAccessResult.Pending => "En attente",
        _ => "Inconnu"
    };

    [Ignore]
    public bool IsAuthorized => Result == NfcAccessResult.Authorized;

    [Ignore]
    public bool IsPending => Result == NfcAccessResult.Pending;

    // Rétro-compatibilité : ancien champ Timestamp redirigé vers TimestampUtc
    [Ignore]
    public DateTime Timestamp
    {
        get => TimestampUtc.ToLocalTime();
        set => TimestampUtc = value.ToUniversalTime();
    }

    // Rétro-compatibilité : ancien champ Message redirigé vers Reason
    [Ignore]
    public string? Message
    {
        get => Reason;
        set => Reason = value;
    }
}

/// <summary>Résultat d'un accès NFC.</summary>
public enum NfcAccessResult
{
    Pending = 0,
    Authorized = 1,
    Denied = 2
}
