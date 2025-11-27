# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["TingoAI.PaymentGateway.sln", "./"]
COPY ["src/TingoAI.PaymentGateway.Domain/TingoAI.PaymentGateway.Domain.csproj", "src/TingoAI.PaymentGateway.Domain/"]
COPY ["src/TingoAI.PaymentGateway.Application/TingoAI.PaymentGateway.Application.csproj", "src/TingoAI.PaymentGateway.Application/"]
COPY ["src/TingoAI.PaymentGateway.Infrastructure/TingoAI.PaymentGateway.Infrastructure.csproj", "src/TingoAI.PaymentGateway.Infrastructure/"]
COPY ["src/TingoAI.PaymentGateway.API/TingoAI.PaymentGateway.API.csproj", "src/TingoAI.PaymentGateway.API/"]

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/src/TingoAI.PaymentGateway.API"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80

# Run the application
ENTRYPOINT ["dotnet", "TingoAI.PaymentGateway.API.dll"]
