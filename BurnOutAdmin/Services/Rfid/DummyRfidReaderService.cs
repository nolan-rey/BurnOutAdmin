namespace BurnOutAdmin.Services.Rfid;

/// <summary>
/// Implémentation factice du lecteur RFID pour le développement sur macOS/iOS/Android.
/// Permet de simuler un scan via la méthode <see cref="SimulateScan"/>.
/// </summary>
public class DummyRfidReaderService : IRfidReaderService
{
    /// <inheritdoc />
    public event EventHandler<string>? UidReceived;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public Task StartAsync()
    {
        IsConnected = true;
        System.Diagnostics.Debug.WriteLine("[DummyRfid] Lecteur factice démarré.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        IsConnected = false;
        System.Diagnostics.Debug.WriteLine("[DummyRfid] Lecteur factice arrêté.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Simule la lecture d'un badge NFC avec l'UID spécifié.
    /// Utilisé pour les tests et le développement sans matériel.
    /// </summary>
    /// <param name="uid">UID à simuler (ex: "04:A3:5B:12").</param>
    public void SimulateScan(string uid)
    {
        if (!IsConnected)
        {
            System.Diagnostics.Debug.WriteLine("[DummyRfid] Impossible de simuler : lecteur non démarré.");
            return;
        }

        if (string.IsNullOrWhiteSpace(uid))
            return;

        System.Diagnostics.Debug.WriteLine($"[DummyRfid] Scan simulé : {uid}");
        UidReceived?.Invoke(this, uid.Trim());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        IsConnected = false;
        GC.SuppressFinalize(this);
    }
}
