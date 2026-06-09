namespace BurnOutAdmin.Models;

public class Client
{
    public int Id { get; set; }

    /// <summary>Firebase UID (ex: "T3Ibaxq1aMcipHNYBBhBgZcfF3l2") — sert à résoudre l'ID SQL réel.</summary>
    public string? FirebaseUid { get; set; }

    // ── Identité ──────────────────────────────────────────────────
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? NfcUid { get; set; }
    public Subscription? Subscription { get; set; }

    /// <summary>URL Cloudinary de la photo de profil (peut être null).</summary>
    public string? ImageUrl { get; set; }

    // ── Coordonnées ───────────────────────────────────────────────
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // ── Profil personnel ──────────────────────────────────────────
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? Height { get; set; }      // cm
    public double? Weight { get; set; }      // kg
    public string? BloodType { get; set; }
    public string? MembershipLevel { get; set; }
    public int? MemberSince { get; set; }
    public string? FavoriteSport { get; set; }

    /// <summary>Texte libre de présentation/bio renseigné par le client à l'inscription.</summary>
    public string? Presentation { get; set; }
    public bool HasPresentation => !string.IsNullOrWhiteSpace(Presentation);

    /// <summary>Identifiant du totem attribué au client (NULL si non choisi).</summary>
    public int? TotemRang { get; set; }

    /// <summary>Vue prête à afficher du totem du client (image, nom, couleurs).</summary>
    public Totem? Totem => Totem.ByRank(TotemRang);
    public bool HasTotem => Totem is not null;

    // ── Habitudes / état ──────────────────────────────────────────
    public string? PhysicalFatigue { get; set; }
    public string? MentalFatigue { get; set; }
    public string? SleepQuality { get; set; }
    public string? SleepQuantity { get; set; }
    public string? DietQuality { get; set; }
    public string? DietQuantity { get; set; }
    public string? TobaccoConsumption { get; set; }
    public string? AlcoholConsumption { get; set; }

    // ── Médical ───────────────────────────────────────────────────
    public string? MovementLimitations { get; set; }
    public string? MedicalCondition { get; set; }
    public string? SpecificTreatment { get; set; }

    // ── Performance cardio ────────────────────────────────────────
    public int? MaxHeartRate { get; set; }
    public int? RestHeartRate { get; set; }

    // ── Statistiques globales ─────────────────────────────────────
    public int? WeeklyVisits { get; set; }
    public int? CaloriesBurned { get; set; }

    public DateTime? CreatedAt { get; set; }

    // ── Helpers computed ──────────────────────────────────────────
    public string FullName       => $"{LastName}, {FirstName}";
    public bool   HasNfcCard     => !string.IsNullOrEmpty(NfcUid);
    public string NfcDisplayText => HasNfcCard ? NfcUid! : "Aucune carte associée";
    public bool   HasProfileImage => !string.IsNullOrWhiteSpace(ImageUrl);

    /// <summary>"35 ans" si BirthDate connue, sinon null.</summary>
    public int? AgeYears
    {
        get
        {
            if (BirthDate is not { } b) return null;
            var today = DateTime.Today;
            var age = today.Year - b.Year;
            if (b.Date > today.AddYears(-age)) age--;
            return age < 0 ? null : age;
        }
    }

    public string FullAddress
    {
        get
        {
            var parts = new[] { Address, $"{PostalCode} {City}".Trim(), Country }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
            return parts.Length == 0 ? string.Empty : string.Join(", ", parts);
        }
    }

    /// <summary>IMC arrondi à 1 décimale, ou null si données manquantes.</summary>
    public double? BodyMassIndex
    {
        get
        {
            if (Height is not > 0 || Weight is not > 0) return null;
            var heightMeters = Height.Value / 100.0;
            return Math.Round(Weight.Value / (heightMeters * heightMeters), 1);
        }
    }
}
