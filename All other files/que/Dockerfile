#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TT.Core.Telemetry.WebJob/TT.Core.Telemetry.WebJob.csproj", "TT.Core.Telemetry.WebJob/"]
COPY ["TT.Core.Repository.Sql/TT.Core.Repository.Sql.csproj", "TT.Core.Repository.Sql/"]
COPY ["TT.Core.Models/TT.Core.Models.csproj", "TT.Core.Models/"]
COPY ["TT.Core.Services/TT.Core.Services.csproj", "TT.Core.Services/"]
COPY ["TT.Core.StoreProcedure.Sql/TT.Core.StoreProcedure.Sql.csproj", "TT.Core.StoreProcedure.Sql/"]
COPY ["TT.Core.KairosApi/TT.Core.KairosApi.csproj", "TT.Core.KairosApi/"]
COPY ["TT.Core.Repository/TT.Core.Repository.csproj", "TT.Core.Repository/"]
RUN dotnet restore "TT.Core.Telemetry.WebJob/TT.Core.Telemetry.WebJob.csproj"
COPY . .
WORKDIR "/src/TT.Core.Telemetry.WebJob"
RUN dotnet build "TT.Core.Telemetry.WebJob.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TT.Core.Telemetry.WebJob.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN mkdir -p /var/log/app
COPY --from=publish /app/publish .
RUN chmod +x healthcheck.sh
ENTRYPOINT ["dotnet", "TT.Core.Telemetry.WebJob.dll"]