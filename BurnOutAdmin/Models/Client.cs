namespace BurnOutAdmin.Models;

public class Client
{
    public int Id { get; set; }

    /// <summary>Firebase UID (ex: "T3Ibaxq1aMcipHNYBBhBgZcfF3l2") — sert à résoudre l'ID SQL réel.</summary>
    public string? FirebaseUid { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? NfcUid { get; set; }
    public Subscription? Subscription { get; set; }

    public string FullName => $"{LastName}, {FirstName}";
    public bool HasNfcCard => !string.IsNullOrEmpty(NfcUid);
    public string NfcDisplayText => HasNfcCard ? NfcUid! : "Aucune carte associée";
}
