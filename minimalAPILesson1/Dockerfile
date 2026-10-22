# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["minimalAPILesson1.csproj", "./"]
RUN dotnet restore "minimalAPILesson1.csproj"
COPY . .
RUN dotnet publish "minimalAPILesson1.csproj" -c Release -o /app/publish

# Stage 2: Run the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "minimalAPILesson1.dll"]