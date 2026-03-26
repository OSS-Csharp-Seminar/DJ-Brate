FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["DJBrate.Web/DJBrate.Web.csproj", "DJBrate.Web/"]
COPY ["DJBrate.Application/DJBrate.Application.csproj", "DJBrate.Application/"]
COPY ["DJBrate.Infrastructure/DJBrate.Infrastructure.csproj", "DJBrate.Infrastructure/"]
COPY ["DJBrate.Domain/DJBrate.Domain.csproj", "DJBrate.Domain/"]
RUN dotnet restore "DJBrate.Web/DJBrate.Web.csproj"

COPY . .
WORKDIR "/src/DJBrate.Web"
RUN dotnet publish "DJBrate.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DJBrate.Web.dll"]
