# syntax=docker/dockerfile:1
ARG BASE_IMAGE=ghcr.io/linuxserver/sonarr:latest
FROM ${BASE_IMAGE}

ARG BUILD_DATE
ARG VERSION
ARG SONARR_BRANCH=main
ARG PACKAGE_AUTHOR="github.com/actuallyevan/Sonarr"
ARG SONARR_REPO="actuallyevan/Sonarr"
ARG TARGETARCH

LABEL org.opencontainers.image.created="${BUILD_DATE}" \
  org.opencontainers.image.source="${PACKAGE_AUTHOR}" \
  org.opencontainers.image.version="${VERSION}"

RUN apk add --no-cache curl tar && \
  mkdir -p /app/sonarr/bin /tmp/sonarr && \
  case "$TARGETARCH" in \
    amd64) runtime="linux-musl-x64" ;; \
    arm64) runtime="linux-musl-arm64" ;; \
    *) echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1 ;; \
  esac && \
  curl -fsSL -o /tmp/sonarr/sonarr.tar.gz \
    "https://github.com/${SONARR_REPO}/releases/download/v${VERSION}/Sonarr.${SONARR_BRANCH}.${VERSION}.${runtime}.tar.gz" && \
  rm -rf /app/sonarr/bin/* && \
  tar -xzf /tmp/sonarr/sonarr.tar.gz -C /app/sonarr/bin --strip-components=1 && \
  echo -e "UpdateMethod=docker\nBranch=${SONARR_BRANCH}\nPackageVersion=${VERSION:-LocalBuild}\nPackageAuthor=${PACKAGE_AUTHOR}" > /app/sonarr/package_info && \
  printf "Linuxserver.io version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version && \
  echo "**** cleanup ****" && \
  rm -rf \
    /app/sonarr/bin/Sonarr.Update \
    /tmp/*
