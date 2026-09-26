# API MixoLogger (.NET). Construite depuis la racine du dépôt : global.json y épingle le SDK.
# À la sortie de .NET 11 (10/11/2026) : passer aux tags 11.0 (docs/MVP.md §10.1).

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-preview.7 AS construction
WORKDIR /src

# Restauration d'abord, sur les seuls fichiers de projet : cette couche reste en cache tant
# qu'aucune dépendance ne change.
COPY global.json ./
COPY MixoLoggerBack/Domain/Domain.csproj MixoLoggerBack/Domain/
COPY MixoLoggerBack/Application/Application.csproj MixoLoggerBack/Application/
COPY MixoLoggerBack/Infrastructure/Infrastructure.csproj MixoLoggerBack/Infrastructure/
COPY MixoLoggerBack/Web/Web.csproj MixoLoggerBack/Web/
RUN dotnet restore MixoLoggerBack/Web/Web.csproj

COPY MixoLoggerBack/Domain/ MixoLoggerBack/Domain/
COPY MixoLoggerBack/Application/ MixoLoggerBack/Application/
COPY MixoLoggerBack/Infrastructure/ MixoLoggerBack/Infrastructure/
COPY MixoLoggerBack/Web/ MixoLoggerBack/Web/
RUN dotnet publish MixoLoggerBack/Web/Web.csproj --configuration Release --no-restore --output /app


FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-preview.7
WORKDIR /app
COPY --from=construction /app ./

# Base SQLite, sauvegardes et clés des cookies : tout dans /app/Donnees, monté en volume.
# Créé ici au nom de l'utilisateur non privilégié, pour qu'un volume neuf en hérite.
RUN mkdir -p /app/Donnees && chown "$APP_UID" /app/Donnees
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__Dossier=Donnees/Cles
EXPOSE 8080
VOLUME /app/Donnees

ENTRYPOINT ["dotnet", "Web.dll"]
