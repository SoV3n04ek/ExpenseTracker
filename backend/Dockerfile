# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["ExpenseTracker.API/ExpenseTracker.API.csproj", "ExpenseTracker.API/"]
COPY ["ExpenseTracker.Application/ExpenseTracker.Application.csproj", "ExpenseTracker.Application/"]
COPY ["ExpenseTracker.Domain/ExpenseTracker.Domain.csproj", "ExpenseTracker.Domain/"]
COPY ["ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj", "ExpenseTracker.Infrastructure/"]

RUN dotnet restore "ExpenseTracker.API/ExpenseTracker.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish
WORKDIR "/src/ExpenseTracker.API"
RUN dotnet publish "ExpenseTracker.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

# Copy the published output
COPY --from=build /app/publish .

# Use non-root user for security (default in .NET 8+ images)
USER app

ENTRYPOINT ["dotnet", "ExpenseTracker.API.dll"]
