# syntax=docker/dockerfile:1.7-labs

########## Build ##########
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

RUN apk add --no-cache clang lld build-base zlib-dev

# Copy solution and restore
COPY Directory.Build.props Directory.Packages.props IssueAgent.sln ./
COPY src ./src
COPY tests ./tests

RUN dotnet restore IssueAgent.sln

# Publish self-contained AOT binary for linux-musl-x64
RUN dotnet publish src/IssueAgent.Action/IssueAgent.Action.csproj \
    --configuration Release \
    --runtime linux-musl-x64 \
    --self-contained true \
    -p:PublishAot=true \
    -p:PublishTrimmed=true \
    -p:InvariantGlobalization=true \
    -p:StripSymbols=true \
    -o /app/publish

########## Runtime ##########
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine AS runtime

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

LABEL org.opencontainers.image.source="https://github.com/mattdot/issueagent" \
    org.opencontainers.image.description="Issue agent action runtime"

WORKDIR /app

COPY --from=build /app/publish/ ./
COPY src/IssueAgent.Action/Scripts/entrypoint.sh /entrypoint.sh

RUN addgroup -S issueagent && adduser -S issueagent -G issueagent \
    && chmod +x /entrypoint.sh \
    && chown -R issueagent:issueagent /app

USER issueagent

ENTRYPOINT ["/entrypoint.sh"]
