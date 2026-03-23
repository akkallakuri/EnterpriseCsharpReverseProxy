FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ControlPlane/EnterpriseCsharpReverseProxy.ControlPlane.csproj", "src/ControlPlane/"]
RUN dotnet restore "src/ControlPlane/EnterpriseCsharpReverseProxy.ControlPlane.csproj"
COPY . .
WORKDIR "/src/src/ControlPlane"
RUN dotnet build "EnterpriseCsharpReverseProxy.ControlPlane.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EnterpriseCsharpReverseProxy.ControlPlane.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
# Run as non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup appuser
USER appuser
COPY --from=publish --chown=appuser:appgroup /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseCsharpReverseProxy.ControlPlane.dll"]
