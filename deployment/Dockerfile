FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TerziTuran.Web ./TerziTuran.Web
WORKDIR /src/TerziTuran.Web
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
RUN mkdir -p /app/data
VOLUME ["/app/data"]
EXPOSE 8080
ENTRYPOINT ["dotnet", "TerziTuran.Web.dll"]
