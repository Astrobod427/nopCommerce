# create the build instance 
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build

ARG TARGETPLATFORM
ARG BUILDPLATFORM

WORKDIR /src                                                                    
COPY ./src ./

# build project
RUN dotnet build Presentation/Nop.Web/Nop.Web.csproj -c Release

# publish project
WORKDIR /src/Presentation/Nop.Web   
RUN dotnet publish Nop.Web.csproj -c Release -o /app/published

# Explicitly publish the SimpleApi plugin
WORKDIR /src/Plugins/Nop.Plugin.Api.SimpleApi
RUN dotnet publish Nop.Plugin.Api.SimpleApi.csproj -c Release -o /app/published/Plugins/Nop.Plugin.Api.SimpleApi

WORKDIR /app/published

RUN echo '#!/bin/sh\nfind /app -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null || true' > /clean.sh && chmod +x /clean.sh

RUN mkdir logs bin

RUN chmod 775 App_Data \
    App_Data/DataProtectionKeys \
    bin \
    logs \
    Plugins \
    wwwroot/bundles \
    wwwroot/db_backups \
    wwwroot/files/exportimport \
    wwwroot/icons \
    wwwroot/images \
    wwwroot/images/thumbs \
    wwwroot/images/uploaded \
    wwwroot/sitemaps

# create the runtime instance 
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime 

# add globalization support
RUN apt-get update && apt-get install -y locales && rm -rf /var/lib/apt/lists/*
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# installs required packages
RUN apt-get update && apt-get install -y libtiff6 libgdiplus libc6-dev tzdata && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/published .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Nop.Web.dll"]
