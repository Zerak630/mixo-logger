namespace Domain.Utilisateurs;

/// <summary>
/// Levée quand l'utilisateur connecté tente une action réservée à quelqu'un d'autre, par
/// exemple modifier la recette d'un autre auteur. Traduite en 403.
/// </summary>
public class ActionNonAutoriseeException(string message) : Exception(message);
