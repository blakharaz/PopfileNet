FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PopfileNet.sln", "./"]
COPY ["PopfileNet.Common/PopfileNet.Common.csproj", "PopfileNet.Common/"]
COPY ["PopfileNet.Classifier/PopfileNet.Classifier.csproj", "PopfileNet.Classifier/"]
COPY ["PopfileNet.Database/PopfileNet.Database.csproj", "PopfileNet.Database/"]
COPY ["PopfileNet.Imap/PopfileNet.Imap.csproj", "PopfileNet.Imap/"]
COPY ["PopfileNet.Backend/PopfileNet.Backend.csproj", "PopfileNet.Backend/"]
COPY ["PopfileNet.Ui/PopfileNet.Ui.csproj", "PopfileNet.Ui/"]
COPY ["PopfileNet.Cli/PopfileNet.Cli.csproj", "PopfileNet.Cli/"]

RUN dotnet restore "PopfileNet.sln"

COPY . .
RUN dotnet publish "PopfileNet.Backend/PopfileNet.Backend.csproj" -c Release -o /app/publish/backend
RUN dotnet publish "PopfileNet.Ui/PopfileNet.Ui.csproj" -c Release -o /app/publish/ui

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish/backend /app/backend
COPY --from=build /app/publish/ui /app/ui

RUN chmod +x /app/entrypoint.sh && \
    chown -R 1000:1000 /app

USER 1000:1000

ENTRYPOINT ["/app/entrypoint.sh"]
EXPOSE 5000 5001
