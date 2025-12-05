# Multi-stage Dockerfile for production
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore as distinct layers for better caching
COPY ["LangLearn.Backend.csproj", "./"]
RUN dotnet restore "LangLearn.Backend.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "LangLearn.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Set the URLs the app should listen on and expose port 80
ENV ASPNETCORE_URLS="http://+:80"
EXPOSE 80

# By default the app uses a SQLite file named "LangLearn.db" in the working directory.
# If you want to persist the DB outside the container, mount a volume to "/app" or
# to another path and adjust the connection string accordingly.

# Copy entrypoint script which supports Docker secrets and validates the published DLL
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
