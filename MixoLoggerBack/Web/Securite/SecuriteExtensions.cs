using System.Security.Claims;
using System.Threading.RateLimiting;
using Application.Utilisateurs;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Infrastructure.Securite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Securite;

/// <summary>
/// Authentification par cookie de session, autorisation par défaut, CORS et limitation des
/// tentatives de connexion (cf. docs/MVP.md §8).
/// </summary>
public static class SecuriteExtensions
{
    public const string NomCookie = "mixo_session";
    public const string PolitiqueCors = "Front";
    public const string PolitiqueConnexion = "connexion";
    public const string RevendicationNomAffiche = "nom_affiche";
    public const string RevendicationTampon = "tampon";

    public static IServiceCollection AddSecurite(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environnement)
    {
        // Comptes : section « Comptes » (liste), renseignée hors du dépôt.
        services.AddOptions<ComptesOptions>()
            .Configure(options => options.Liste = configuration.GetSection(ComptesOptions.Section).Get<List<CompteConfigure>>() ?? []);

        services.AddSingleton<IHacheurMotDePasse, HacheurMotDePasse>();
        services.AddHttpContextAccessor();
        services.AddScoped<IUtilisateurCourant, UtilisateurCourant>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = NomCookie;
                // Illisible par le JavaScript : une faille XSS ne peut pas voler la session.
                options.Cookie.HttpOnly = true;
                // Jamais envoyé depuis un autre site : protège contre le CSRF. Le front (:4200)
                // et l'API (:5213) sont le même site en local, le port n'entrant pas en compte.
                options.Cookie.SameSite = SameSiteMode.Strict;
                // En local l'API tourne en HTTP ; partout ailleurs, le cookie exige HTTPS.
                options.Cookie.SecurePolicy = environnement.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;

                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;

                options.Events = new CookieAuthenticationEvents
                {
                    // Une API répond 401 / 403, elle ne redirige pas vers une page de connexion.
                    OnRedirectToLogin = contexte => EcrireProbleme(contexte.HttpContext, StatusCodes.Status401Unauthorized, "Connexion requise"),
                    OnRedirectToAccessDenied = contexte => EcrireProbleme(contexte.HttpContext, StatusCodes.Status403Forbidden, "Accès refusé"),

                    // Une session est rejetée à la requête suivante, au lieu de rester valide jusqu'à
                    // l'expiration du cookie, si le compte a été désactivé (retiré de la configuration)
                    // ou si son mot de passe a changé depuis (tampon différent, ou absent d'une session
                    // ouverte avant que les comptes ne soient en base).
                    OnValidatePrincipal = async contexte =>
                    {
                        string? id = contexte.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        string? tampon = contexte.Principal?.FindFirstValue(RevendicationTampon);
                        IUtilisateurRepository depot = contexte.HttpContext.RequestServices.GetRequiredService<IUtilisateurRepository>();

                        if (!Guid.TryParse(id, out Guid guid)
                            || await depot.GetByIdAsync(guid) is not { } compte
                            || !Guid.TryParse(tampon, out Guid tamponSession)
                            || tamponSession != compte.TamponSecurite)
                        {
                            contexte.RejectPrincipal();
                            await contexte.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    }
                };
            });

        // Tout est protégé par défaut : un endpoint ajouté sans y penser n'est pas public.
        // Les exceptions sont explicites ([AllowAnonymous]).
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        string[] origines = configuration.GetSection("Front:Origines").Get<string[]>() is { Length: > 0 } configurees
            ? configurees
            : ["http://localhost:4200"];

        services.AddCors(options => options.AddPolicy(PolitiqueCors, politique => politique
            .WithOrigins(origines)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Indispensable pour que le navigateur envoie le cookie de session.
            .AllowCredentials()));

        int tentativesParMinute = configuration.GetValue("Securite:TentativesDeConnexionParMinute", 5);

        // Par compte, en plus de la limite par IP : 10 échecs sur 15 minutes par défaut.
        services.AddSingleton(fournisseur => new LimiteurEchecsParCompte(
            configuration.GetValue("Securite:EchecsParCompte", 10),
            TimeSpan.FromMinutes(configuration.GetValue("Securite:FenetreEchecsParCompteMinutes", 15)),
            fournisseur.GetService<TimeProvider>() ?? TimeProvider.System));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (contexte, _) => new ValueTask(EcrireProbleme(
                contexte.HttpContext,
                StatusCodes.Status429TooManyRequests,
                "Trop de tentatives",
                "Trop de tentatives de connexion. Réessaie dans une minute."));

            // Freine le test de mots de passe en série, par adresse IP.
            options.AddPolicy(PolitiqueConnexion, contexte => RateLimitPartition.GetFixedWindowLimiter(
                contexte.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = tentativesParMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }

    /// <summary>
    /// Accorde les comptes en base avec la configuration, après les migrations : une configuration
    /// invalide arrête l'API tout de suite.
    /// </summary>
    public static async Task SynchroniserComptesAsync(this WebApplication app)
    {
        BilanComptes bilan = await app.Services.SynchroniserComptesAsync(
            app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ComptesOptions>>().Value.Liste,
            app.Services.GetRequiredService<IHacheurMotDePasse>());

        if (bilan.Actifs == 0)
            app.Logger.LogWarning(
                "Aucun compte actif : personne ne pourra se connecter. Déclare-les dans la section « {Section} » (cf. docs/MVP.md §10.2).",
                ComptesOptions.Section);
        else
            app.Logger.LogInformation(
                "{Actifs} compte(s) actif(s) ; créés : {Crees}, réactivés : {Reactives}, mots de passe réinitialisés : {Reinitialises}, désactivés : {Desactives}.",
                bilan.Actifs, bilan.Crees, bilan.Reactives, bilan.Reinitialises, bilan.Desactives);

        if (bilan.Reinitialises > 0)
            app.Logger.LogWarning(
                "Mot(s) de passe réinitialisé(s) depuis la configuration : remets « ReinitialiserMotDePasse » à false, sinon chaque démarrage les écrasera.");
    }

    /// <summary>Pose le cookie de session : l'Id, l'identifiant, le nom affiché et le tampon de sécurité du compte.</summary>
    public static Task OuvrirSessionAsync(this HttpContext contexte, UtilisateurDto utilisateur, Guid tamponSecurite)
    {
        ClaimsIdentity identite = new(
            [
                new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
                new Claim(ClaimTypes.Name, utilisateur.Identifiant),
                new Claim(RevendicationNomAffiche, utilisateur.NomAffiche),
                new Claim(RevendicationTampon, tamponSecurite.ToString())
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return contexte.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identite),
            // Session conservée à la fermeture du navigateur, jusqu'à 14 jours d'inactivité.
            new AuthenticationProperties { IsPersistent = true });
    }

    private static Task EcrireProbleme(HttpContext contexte, int statut, string titre, string? detail = null)
    {
        contexte.Response.StatusCode = statut;

        return contexte.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexte,
            ProblemDetails = new ProblemDetails { Status = statut, Title = titre, Detail = detail }
        }).AsTask();
    }
}
