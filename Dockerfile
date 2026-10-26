
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["INVOYFY.WEP/INVOYFY.WEP.csproj", "INVOYFY.WEP/"]
RUN dotnet restore "INVOYFY.WEP/INVOYFY.WEP.csproj"

COPY . .
WORKDIR /src/INVOYFY.WEP
RUN dotnet publish "INVOYFY.WEP.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "INVOYFY.WEP.dll"]
