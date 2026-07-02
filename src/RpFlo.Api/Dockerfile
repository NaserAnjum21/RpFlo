FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RpFlo.Domain/RpFlo.Domain.csproj", "RpFlo.Domain/"]
COPY ["RpFlo.Application/RpFlo.Application.csproj", "RpFlo.Application/"]
COPY ["RpFlo.Infrastructure/RpFlo.Infrastructure.csproj", "RpFlo.Infrastructure/"]
COPY ["RpFlo.Api/RpFlo.Api.csproj", "RpFlo.Api/"]
RUN dotnet restore "RpFlo.Api/RpFlo.Api.csproj"
COPY . .
WORKDIR "/src/RpFlo.Api"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "RpFlo.Api.dll"]
