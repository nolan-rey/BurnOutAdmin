using System.Runtime.InteropServices;

namespace BurnOutAdmin.Platforms.Windows.RFID;

/// <summary>
/// P/Invoke declarations for the StrongLink SL500-USB RFID reader SDK.
/// 
/// DLLs requises : MasterRD.dll (fonctions RF) + MasterCOM.dll (couche COM interne).
/// Les deux DLLs doivent être présentes dans le répertoire de sortie.
///
/// Protocole : ISO14443A (MIFARE Classic / Ultralight / NTAG).
/// Convention d'appel : StdCall (standard pour les DLLs C Windows).
///
/// Retour des fonctions : 0 = succès, autre = code erreur.
/// </summary>
internal static class SL500Native
{
    private const string DllName = "MasterRD.dll";

    /// <summary>
    /// Initialise la communication avec le lecteur SL500 via port COM.
    /// </summary>
    /// <param name="port">Numéro de port COM (0 = COM1, 1 = COM2, 2 = COM3, etc.).</param>
    /// <param name="baud">Vitesse en bauds (ex: 9600).</param>
    /// <returns>Handle du device (icdev) si succès (&gt; 0), ou valeur négative/0 si erreur.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_init_com(int port, int baud);

    /// <summary>
    /// Ferme le port COM et libère les ressources du lecteur.
    /// </summary>
    /// <param name="icdev">Handle retourné par rf_init_com.</param>
    /// <returns>0 si succès.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_closeport(int icdev);

    /// <summary>
    /// Active ou désactive le champ RF (antenne).
    /// </summary>
    /// <param name="icdev">Handle du device.</param>
    /// <param name="mode">1 = activer, 0 = désactiver.</param>
    /// <returns>0 si succès.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_antenna_sta(int icdev, byte mode);

    /// <summary>
    /// Envoie une commande REQUEST ISO14443A pour détecter les cartes à proximité.
    /// </summary>
    /// <param name="icdev">Handle du device.</param>
    /// <param name="reqCode">
    /// Mode de requête :
    ///   0x26 = REQA (cartes en état IDLE uniquement)
    ///   0x52 = REQALL / WUPA (toutes les cartes, y compris HALT)
    /// </param>
    /// <param name="tagType">Buffer de 2 octets recevant l'ATQA (Answer To Request type A).</param>
    /// <returns>0 si carte détectée, autre si aucune carte.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_request(int icdev, byte reqCode, byte[] tagType);

    /// <summary>
    /// Effectue l'anti-collision ISO14443A et récupère l'UID de la carte.
    /// </summary>
    /// <param name="icdev">Handle du device.</param>
    /// <param name="bcnt">Nombre de bits connus (0x04 pour anti-collision standard niveau 1).</param>
    /// <param name="snr">Buffer de 4 octets recevant l'UID (Serial Number).</param>
    /// <returns>0 si succès.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_anticoll(int icdev, byte bcnt, byte[] snr);

    /// <summary>
    /// Sélectionne la carte identifiée par son UID pour les opérations ultérieures.
    /// </summary>
    /// <param name="icdev">Handle du device.</param>
    /// <param name="snr">UID de 4 octets (obtenu par rf_anticoll).</param>
    /// <param name="size">Buffer de 1 octet recevant la taille (SAK — Select Acknowledge).</param>
    /// <returns>0 si succès.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_select(int icdev, byte[] snr, byte[] size);

    /// <summary>
    /// Met la carte en état HALT (arrêt de communication).
    /// Utile pour libérer la carte et permettre une nouvelle détection via REQALL.
    /// </summary>
    /// <param name="icdev">Handle du device.</param>
    /// <returns>0 si succès.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_halt(int icdev);

    /// <summary>
    /// Active le buzzer du lecteur (signal sonore).
    /// </summary>
    /// <param name="icdev">Handle du device.</param>
    /// <param name="msec">Durée du bip en dizaines de millisecondes (ex: 10 = 100ms).</param>
    /// <returns>0 si succès.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int rf_beep(int icdev, int msec);
}
