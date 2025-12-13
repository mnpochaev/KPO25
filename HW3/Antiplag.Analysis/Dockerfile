FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Antiplag.sln ./
COPY Antiplag.FileStorage/Antiplag.FileStorage.csproj Antiplag.FileStorage/
COPY Antiplag.Analysis/Antiplag.Analysis.csproj Antiplag.Analysis/
COPY Antiplag.Gateway/Antiplag.Gateway.csproj Antiplag.Gateway/

RUN dotnet restore Antiplag.Analysis/Antiplag.Analysis.csproj

COPY . .
RUN dotnet publish Antiplag.Analysis/Antiplag.Analysis.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Antiplag.Analysis.dll"]