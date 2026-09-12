namespace Domain.Interfaces.Repositories;

/// <summary>
/// Levée quand une écriture repose sur une version périmée de l'agrégat : quelqu'un
/// d'autre l'a modifié entre la lecture et la sauvegarde.
/// </summary>
/// <remarks>
/// Hérite d'<see cref="InvalidOperationException"/> pour être traduite en 409 comme les
/// autres opérations impossibles. Le client relit l'état et rejoue sa modification.
/// </remarks>
public class ConflitDeConcurrenceException(string message) : InvalidOperationException(message);
