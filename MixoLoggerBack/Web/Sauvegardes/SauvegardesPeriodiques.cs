using Infrastructure.Persistance;

namespace Web.Sauvegardes;

/// <summary>
/// Sauvegarde automatique de la base : vérifie au démarrage puis toutes les heures si la dernière
/// copie a dépassé l'intervalle configuré (24 h par défaut). Une API redémarrée souvent sauvegarde
/// donc quand même, et une API qui tourne des semaines aussi.
/// </summary>
public class SauvegardesPeriodiques(SauvegardeBase sauvegarde, ILogger<SauvegardesPeriodiques> journal) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken arret)
    {
        if (!sauvegarde.Options.Active)
        {
            journal.LogWarning("Sauvegardes automatiques désactivées (« {Section}:Active »).", SauvegardeOptions.Section);
            return;
        }

        TimeSpan verification = sauvegarde.Options.Intervalle < TimeSpan.FromHours(1) ? sauvegarde.Options.Intervalle : TimeSpan.FromHours(1);
        using PeriodicTimer minuterie = new(verification);

        do
        {
            try
            {
                await sauvegarde.SauvegarderSiNecessaireAsync(arret);
            }
            catch (Exception erreur) when (!arret.IsCancellationRequested)
            {
                // Disque plein, dossier interdit… : l'API continue, l'erreur est journalisée et on réessaie.
                journal.LogError(erreur, "Échec de la sauvegarde automatique de la base.");
            }
        }
        while (await minuterie.WaitForNextTickAsync(arret));
    }
}
