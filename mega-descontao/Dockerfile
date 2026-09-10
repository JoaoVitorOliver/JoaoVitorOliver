# Uma imagem só: a API serve a API e a vitrine. Mesma origem, sem CORS, um processo.

FROM node:22-bookworm-slim AS front
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/MegaDescontao.Api/MegaDescontao.Api.csproj ./MegaDescontao.Api/
RUN dotnet restore MegaDescontao.Api/MegaDescontao.Api.csproj
COPY backend/MegaDescontao.Api/ ./MegaDescontao.Api/
RUN dotnet publish MegaDescontao.Api/MegaDescontao.Api.csproj -c Release -o /app --no-restore

# Imagem Debian de propósito: a busca sem acento usa String.Normalize, que depende do ICU.
# A variante Alpine não traz ICU e a busca por "tenis" pararia de achar "Tênis".
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=api /app ./
COPY --from=front /src/dist ./wwwroot

# O banco fica num volume: sem isso, cada deploy apagaria o catálogo e o histórico de cliques.
VOLUME /data
ENV ConnectionStrings__Default="Data Source=/data/megadescontao.db"

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MegaDescontao.Api.dll"]
