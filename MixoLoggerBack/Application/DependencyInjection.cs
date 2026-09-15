using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    /// <summary>
    /// Handlers MediatR. Les dépôts sont enregistrés par l'infrastructure
    /// (<c>AddPersistance</c>) : l'application ne dépend que de leurs interfaces.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
		services.AddMediatR(cfg =>
		{
			cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
			//cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
		});

        return services;
    }
}
