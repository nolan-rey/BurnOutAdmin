namespace BurnOutAdmin.Models.Program;

/// <summary>
/// Unité d'affichage de la durée en mode temps (<see cref="ExerciseModel.IsTimeMode"/>).
/// Côté base, la durée est toujours stockée en <b>secondes</b> ; l'unité ne sert
/// qu'à la saisie/affichage côté admin et à la conversion au moment du save.
/// </summary>
public enum TimeUnit
{
    Seconds,
    Minutes
}
