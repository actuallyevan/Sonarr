# syntax=docker/dockerfile:1
ARG BASE_IMAGE=ghcr.io/linuxserver/sonarr:latest

FROM scratch AS binaries
ARG TARGETARCH
COPY _artifacts/linux-musl-x64/net6.0/Sonarr /amd64
COPY _artifacts/linux-musl-arm64/net6.0/Sonarr /arm64

FROM ${BASE_IMAGE}

ARG BUILD_DATE
ARG VERSION
ARG SONARR_BRANCH=main
ARG PACKAGE_AUTHOR="github.com/actuallyevan/Sonarr"
ARG TARGETARCH

LABEL org.opencontainers.image.created="${BUILD_DATE}" \
  org.opencontainers.image.source="${PACKAGE_AUTHOR}" \
  org.opencontainers.image.version="${VERSION}"

COPY --from=binaries /${TARGETARCH}/. /app/sonarr/bin/

RUN echo -e "UpdateMethod=docker\nBranch=${SONARR_BRANCH}\nPackageVersion=${VERSION:-LocalBuild}\nPackageAuthor=${PACKAGE_AUTHOR}" > /app/sonarr/package_info && \
  printf "Linuxserver.io version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version && \
  echo "**** cleanup ****" && \
  rm -rf \
    /app/sonarr/bin/Sonarr.Update
