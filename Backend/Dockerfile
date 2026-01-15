# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Podium/Podium.API/Podium.API.csproj", "Podium.API/"]
COPY ["Podium/Podium.Application/Podium.Application.csproj", "Podium.Application/"]
COPY ["Podium/Podium.Core/Podium.Core.csproj", "Podium.Core/"]
COPY ["Podium/Podium.Infrastructure/Podium.Infrastructure.csproj", "Podium.Infrastructure/"]

RUN dotnet restore "Podium.API/Podium.API.csproj"

# Copy everything else and build
COPY Podium/ .
WORKDIR "/src/Podium.API"
RUN dotnet build "Podium.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Podium.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 podiumgroup && \
    adduser --system --uid 1001 --ingroup podiumgroup podiumuser

# Create logs directory with correct permissions
RUN mkdir -p /app/logs && chown -R podiumuser:podiumgroup /app/logs

# Copy published app
COPY --from=publish /app/publish .

# Set ownership of app files
RUN chown -R podiumuser:podiumgroup /app

# Switch to non-root user
USER podiumuser

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Podium.API.dll"]
