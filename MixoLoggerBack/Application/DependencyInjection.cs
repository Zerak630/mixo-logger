using System.Reflection;
using Domain.Interfaces.Repositories;
using Infrastructure.Repositories;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
		// Configuration de MediatR
		services.AddMediatR(cfg =>
		{
			cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
			//cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
		});

        // Enregistrement des services d'application
        services.AddScoped<ICocktailRepository, CocktailRepository>();
        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IBarRepository, BarRepository>();

        return services;
    }
}