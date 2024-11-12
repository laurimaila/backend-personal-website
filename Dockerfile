FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH

WORKDIR /src

# Copy .sln and .csproj and restore dependencies
COPY *.sln .
COPY backend/*.csproj ./backend/
RUN dotnet restore -a $TARGETARCH

# Copy the source code and build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore -a $TARGETARCH

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create a non-root user
RUN addgroup --system --gid 1001 dotnet && \
    adduser --system --uid 1001 appuser

# Copy published files from the build stage
COPY --from=build --chown=appuser:dotnet /app/publish .

# Configure production environment
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

USER appuser

ENTRYPOINT ["dotnet", "backend.dll"]
