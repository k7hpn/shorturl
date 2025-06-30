# Get build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy source
COPY . ./

# Restore packages
RUN dotnet restore

# Build project and run tests
RUN dotnet test -v m /property:WarningLevel=0

# Publish
WORKDIR /app/ShortURL
RUN dotnet publish -v m /property:WarningLevel=0 -c Release --property:PublishDir=/app/publish

# Get runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS publish

WORKDIR /app

# Bring in metadata via --build-arg
ARG BRANCH=unknown
ARG IMAGE_CREATED=unknown
ARG IMAGE_REVISION=unknown
ARG IMAGE_VERSION=unknown

# Configure image labels
LABEL branch=$branch \
    maintainer="Maricopa County Library District developers <development@mcldaz.org>" \
    org.opencontainers.image.authors="Maricopa County Library District developers <development@mcldaz.org>" \
    org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.description="ShortURL is a cross-platform Web-based application for creating URLs and tracking click counts." \
    org.opencontainers.image.documentation="https://github.com/MCLD/ShortURL" \
    org.opencontainers.image.licenses="MIT" \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.source="https://github.com/MCLD/ShortURL" \
    org.opencontainers.image.title="ShortURL" \
    org.opencontainers.image.url="https://github.com/MCLD/ShortURL" \
    org.opencontainers.image.vendor="Maricopa County Library District" \
    org.opencontainers.image.version=$IMAGE_VERSION

# Default image environment variable settings
ENV org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.version=$IMAGE_VERSION

# Copy source
COPY --from=build "/app/publish" .

# Port 8080 for http
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["dotnet", "ShortURL.dll"]

