FROM dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false

RUN dotnet nuget add source 'http://172.22.227.36:8081/repository/nuget.org-proxy/' -n nuget-nexux.org
RUN dotnet nuget disable source "nuget.org"
RUN dotnet restore "autorizadora_producer.csproj"

FROM build AS publish
RUN dotnet publish "autorizadora_producer.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "autorizadora_producer.dll"]