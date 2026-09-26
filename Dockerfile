FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ConejitoTicket.Api/ConejitoTicket.Api.csproj src/ConejitoTicket.Api/
RUN dotnet restore src/ConejitoTicket.Api/ConejitoTicket.Api.csproj
COPY . .
RUN dotnet publish src/ConejitoTicket.Api/ConejitoTicket.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ConejitoTicket.Api.dll"]
