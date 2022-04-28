FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ProcessModelingTransformationEngine/ProcessModelingTransformationEngine.csproj", "ProcessModelingTransformationEngine/"]
RUN dotnet restore "ProcessModelingTransformationEngine/ProcessModelingTransformationEngine.csproj"
COPY . .
WORKDIR "/src/ProcessModelingTransformationEngine"
RUN dotnet build "ProcessModelingTransformationEngine.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProcessModelingTransformationEngine.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProcessModelingTransformationEngine.dll"]
