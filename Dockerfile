FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LibraryManagement.csproj ./
RUN dotnet restore LibraryManagement.csproj

COPY . ./
RUN dotnet publish LibraryManagement.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV PORT=8080
EXPOSE 8080
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "LibraryManagement.dll"]
