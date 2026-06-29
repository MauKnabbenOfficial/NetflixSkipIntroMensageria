# ── Estágio 1: build ──────────────────────────────────────────────────────────
# Usa a imagem SDK completa para compilar o projeto e suas dependências.
# O contexto do build DEVE ser a raiz da solution (não a pasta do projeto),
# porque há ProjectReferences para Catalog, Infrastructure, etc.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia todos os .csproj primeiro — camada cacheável pelo Docker.
# Se só o código mudar (não os pacotes), esta camada é reutilizada.
COPY src/NetflixSkipIntroMensageria.API/NetflixSkipIntroMensageria.API.csproj                         src/NetflixSkipIntroMensageria.API/
COPY src/NetflixSkipIntroMensageria.Catalog/NetflixSkipIntroMensageria.Catalog.csproj                 src/NetflixSkipIntroMensageria.Catalog/
COPY src/NetflixSkipIntroMensageria.Infrastructure/NetflixSkipIntroMensageria.Infrastructure.csproj   src/NetflixSkipIntroMensageria.Infrastructure/
COPY src/NetflixSkipIntroMensageria.SharedKernel/NetflixSkipIntroMensageria.SharedKernel.csproj       src/NetflixSkipIntroMensageria.SharedKernel/
COPY src/NetflixSkipIntroMensageria.Streaming/NetflixSkipIntroMensageria.Streaming.csproj             src/NetflixSkipIntroMensageria.Streaming/

RUN dotnet restore src/NetflixSkipIntroMensageria.API/NetflixSkipIntroMensageria.API.csproj

# Copia o restante do código-fonte
COPY src/ src/

# Publica em modo Release na pasta /app/publish
RUN dotnet publish src/NetflixSkipIntroMensageria.API/NetflixSkipIntroMensageria.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Estágio 2: runtime ────────────────────────────────────────────────────────
# Imagem mínima (aspnet), sem SDK. Resultado final muito menor (~250 MB vs ~900 MB).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Usuário não-root por segurança (boa prática em produção)
USER app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "NetflixSkipIntroMensageria.API.dll"]
