#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 5001 

FROM mcr.microsoft.com/dotnet/sdk:5.0.102-ca-patch-buster-slim AS build   
WORKDIR /src
COPY ["ZyzzyvagRPC/ZyzzyvagRPC.csproj", "ZyzzyvagRPC/"]
RUN dotnet restore "ZyzzyvagRPC/ZyzzyvagRPC.csproj"
COPY . .
WORKDIR "/src/ZyzzyvagRPC"
RUN dotnet build "ZyzzyvagRPC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZyzzyvagRPC.csproj" -c Release -o /app/publish

FROM base AS final
ENV ASPNETCORE_URLS="https://+;http://+"
ENV ASPNETCORE_HTTPS_PORT=443 
ENV ASPNETCORE_Kestrel__Certificates__Default__Password="test123" 
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx 
WORKDIR /
COPY ["ZyzzyvagRPC/aspnetapp.pfx", "/https/aspnetapp.pfx"]
WORKDIR /app
COPY --from=publish /app .
COPY ["ZyzzyvagRPC/ActorHocon.hocon", "/app/config/ActorHocon.hocon"]
ENTRYPOINT ["dotnet", "publish/ZyzzyvagRPC.dll"]