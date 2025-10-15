# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory to where the project is
WORKDIR /app

# Copy the build script
COPY build.sh /app/build.sh
RUN chmod +x /app/build.sh

# The output will be in /app/publish and can be copied to the host
