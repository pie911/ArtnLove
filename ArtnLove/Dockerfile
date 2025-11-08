FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ArtnLove/ArtnLove.csproj", "ArtnLove/"]
RUN dotnet restore "ArtnLove/ArtnLove.csproj"
COPY . .
WORKDIR "/src/ArtnLove"
RUN dotnet publish "ArtnLove.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArtnLove.dll"]
