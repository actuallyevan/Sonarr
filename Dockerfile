# syntax=docker/dockerfile:1
ARG BASE_IMAGE=ghcr.io/linuxserver/sonarr:latest
FROM ${BASE_IMAGE}

ARG BUILD_DATE
ARG VERSION
ARG SONARR_BRANCH=main
ARG PACKAGE_AUTHOR="github.com/actuallyevan/Sonarr"
ARG TARGETARCH

LABEL org.opencontainers.image.created="${BUILD_DATE}" \
  org.opencontainers.image.source="${PACKAGE_AUTHOR}" \
  org.opencontainers.image.version="${VERSION}"

COPY _artifacts/linux-musl-x64/net6.0/Sonarr /tmp/sonarr-x64
COPY _artifacts/linux-musl-arm64/net6.0/Sonarr /tmp/sonarr-arm64

RUN mkdir -p /app/sonarr/bin && \
  if [ "$TARGETARCH" = "amd64" ]; then \
    cp -r /tmp/sonarr-x64/* /app/sonarr/bin/; \
  elif [ "$TARGETARCH" = "arm64" ]; then \
    cp -r /tmp/sonarr-arm64/* /app/sonarr/bin/; \
  else \
    echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1; \
  fi && \
  rm -rf /tmp/sonarr-x64 /tmp/sonarr-arm64 && \
  echo -e "UpdateMethod=docker\nBranch=${SONARR_BRANCH}\nPackageVersion=${VERSION:-LocalBuild}\nPackageAuthor=${PACKAGE_AUTHOR}" > /app/sonarr/package_info && \
  printf "Linuxserver.io version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version && \
  echo "**** cleanup ****" && \
  rm -rf \
    /app/sonarr/bin/Sonarr.Update
