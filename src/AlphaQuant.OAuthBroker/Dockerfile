FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/AlphaQuant.Core/ src/AlphaQuant.Core/
COPY src/AlphaQuant.Infrastructure/ src/AlphaQuant.Infrastructure/
COPY src/AlphaQuant.OAuthBroker/ src/AlphaQuant.OAuthBroker/
RUN dotnet publish src/AlphaQuant.OAuthBroker/AlphaQuant.OAuthBroker.csproj -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "AlphaQuant.OAuthBroker.dll"]
