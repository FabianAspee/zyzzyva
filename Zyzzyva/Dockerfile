#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app 

FROM mcr.microsoft.com/dotnet/sdk:5.0.102-ca-patch-buster-slim AS build 
WORKDIR /src
COPY ["Zyzzyva/Zyzzyva.csproj","Zyzzyva/"]
RUN dotnet restore "Zyzzyva/Zyzzyva.csproj"
COPY . . 
WORKDIR "/src/Zyzzyva"
RUN dotnet build "Zyzzyva.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Zyzzyva.csproj" -c Release -o /app/publish


FROM base AS final  
WORKDIR /app
COPY --from=publish /app .
COPY ["Zyzzyva/ActorHocon.hocon", "/app/config/ActorHocon.hocon"]
COPY ["Zyzzyva/Database/Settings/dbconfig.hocon", "/app/config/dbconfig.hocon"]
COPY ["Zyzzyva/Database/DB/databaseZyzzyva.json", "/app/DB/databaseZyzzyva.json"]
ENTRYPOINT ["dotnet", "publish/Zyzzyva.dll"]
 