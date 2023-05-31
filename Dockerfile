# Set the base image to the official .NET Core 6.0 SDK image from Microsoft
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

# Set the working directory to /app
WORKDIR /app

# Copy the .csproj and restore any dependencies (cache restore layer)
COPY *.csproj ./
RUN dotnet restore

# Copy the entire project and build the application (build layer)
COPY . ./
RUN dotnet publish -c Release -o out

# Create a new image for the runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .


# Start the application
ENTRYPOINT ["dotnet", "CartMicroservice.dll"]