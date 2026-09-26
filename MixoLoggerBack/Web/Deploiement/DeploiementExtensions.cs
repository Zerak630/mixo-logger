using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace Web.Deploiement;

/// <summary>
/// Ce qui ne sert qu'une fois l'API déployée derrière un reverse proxy (cf. docs/DEPLOIEMENT.md) :
/// en-têtes transférés et clés de chiffrement des cookies conservées d'un démarrage à l'autre.
/// Sans configuration, rien ne change : le développement local se passe des deux.
/// </summary>
public static class DeploiementExtensions
{
    /// <summary>Réseaux (notation CIDR) d'où les en-têtes <c>X-Forwarded-*</c> sont crus : ceux du reverse proxy.</summary>
    public const string SectionReseauxDeConfiance = "ReverseProxy:ReseauxDeConfiance";

    /// <summary>Dossier des clés de chiffrement des cookies, relatif au dossier de l'API.</summary>
    public const string CleDossierCles = "DataProtection:Dossier";

    public static IServiceCollection AddDeploiement(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environnement)
    {
        // Derrière le proxy, l'API voit l'adresse du proxy et du HTTP. Les en-têtes transférés rendent
        // l'adresse du client (la limitation par IP ne bloque plus tout le monde à la fois) et le schéma
        // https. Ils ne sont crus que depuis les réseaux déclarés : sans cela, n'importe quel client
        // pourrait se faire passer pour une autre adresse en envoyant lui-même X-Forwarded-For.
        string[] reseaux = configuration.GetSection(SectionReseauxDeConfiance).Get<string[]>() ?? [];
        if (reseaux.Length > 0)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // Un seul saut : le proxy. Une chaîne plus longue viendrait du client, donc invérifiable.
                options.ForwardLimit = 1;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();

                foreach (string reseau in reseaux)
                {
                    if (!System.Net.IPNetwork.TryParse(reseau, out System.Net.IPNetwork reseauDeConfiance))
                        throw new InvalidOperationException($"Configuration « {SectionReseauxDeConfiance} » : « {reseau} » n'est pas un réseau CIDR valide (ex. 172.30.0.0/24).");

                    options.KnownIPNetworks.Add(reseauDeConfiance);
                }
            });
        }

        // Les cookies de session sont chiffrés avec des clés que l'API génère. Conservées dans le conteneur,
        // elles disparaîtraient à chaque redéploiement : tout le monde serait déconnecté.
        if (configuration[CleDossierCles] is { Length: > 0 } dossier)
        {
            services.AddDataProtection()
                .SetApplicationName("MixoLogger")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(Path.Combine(environnement.ContentRootPath, dossier))));
        }

        return services;
    }

    /// <summary>En tête du pipeline : tout ce qui suit (limitation, cookies) doit voir le vrai client.</summary>
    public static WebApplication UseDeploiement(this WebApplication app)
    {
        if (app.Configuration.GetSection(SectionReseauxDeConfiance).Get<string[]>() is { Length: > 0 })
            app.UseForwardedHeaders();

        return app;
    }
}
