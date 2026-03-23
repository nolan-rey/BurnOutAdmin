namespace BurnOutAdmin.Services.Rfid;

/// <summary>
/// Abstraction du lecteur RFID physique.
/// Permet de découpler le matériel (port série) du reste de l'application.
/// </summary>
public interface IRfidReaderService : IDisposable
{
    /// <summary>Déclenché lorsqu'un UID est lu par le lecteur.</summary>
    event EventHandler<string>? UidReceived;

    /// <summary>Démarre l'écoute du lecteur RFID.</summary>
    Task StartAsync();

    /// <summary>Arrête l'écoute du lecteur RFID.</summary>
    Task StopAsync();

    /// <summary>Indique si le lecteur est connecté et en écoute.</summary>
    bool IsConnected { get; }
}
