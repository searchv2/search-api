# syntax=docker/dockerfile:1
#
#   docker build -t searchv2-search-api .
#   docker run --rm -p 5071:8080 \
#     -e SEARCH_DB_PATH=/data/db/searchmedium.db \
#     -v "$PWD/../db:/data/db" -v "$PWD/../seData/medium:/data/docs:ro" \
#     searchv2-search-api
#
# The indexer must have written the SQLite index first. Case-sensitive search
# re-reads source files by the path stored in the index, so mount the document
# folder at the same path the indexer used (/data/docs).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG SEARCH_UTILITIES_REF=main
ADD https://github.com/searchv2/search-utilities.git#${SEARCH_UTILITIES_REF} /src/search-utilities
RUN dotnet pack /src/search-utilities/SearchUtilities.csproj -c Release -o /src/search-utilities/nupkg

WORKDIR /src/SearchAPI
COPY . .
RUN dotnet publish SearchAPI.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080 \
    SEARCH_DB_PATH=/data/db/searchmedium.db
EXPOSE 8080

ENTRYPOINT ["dotnet", "SearchAPI.dll"]
