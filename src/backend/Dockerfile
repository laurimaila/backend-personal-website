FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH

WORKDIR /src

# Copy .sln and .csproj and restore dependencies
COPY *.slnx .
COPY src/backend/*.csproj ./src/backend/
RUN dotnet restore ./src/backend/backend.csproj -a $TARGETARCH

# Copy the source code and build
COPY . .
RUN dotnet publish src/backend/backend.csproj -c Release -o /app/publish --no-restore -a $TARGETARCH

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Create a non-root user
RUN addgroup --system --gid 1001 dotnet && \
    adduser --system --uid 1001 appuser

# Copy published files from the build stage
COPY --from=build --chown=appuser:dotnet /app/publish .

# Configure production environment
ENV ASPNETCORE_ENVIRONMENT=Production

ARG BACKEND_VERSION=0.0.0
ENV BACKEND_VERSION=$BACKEND_VERSION

USER appuser

ENTRYPOINT ["dotnet", "backend.dll"]
