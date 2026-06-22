# syntax=docker/dockerfile:1
# Build đa kiến trúc: chạy trên máy nào sẽ ra image cho kiến trúc đó
# (Oracle Ampere = arm64). Multi-stage để image cuối nhỏ gọn.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj trước để tận dụng cache lớp restore.
COPY MeUp.Api/MeUp.Api.csproj MeUp.Api/
RUN dotnet restore MeUp.Api/MeUp.Api.csproj

# Copy phần còn lại của API rồi publish.
COPY MeUp.Api/ MeUp.Api/
RUN dotnet publish MeUp.Api/MeUp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
# Lắng nghe HTTP nội bộ; Cloudflare Tunnel lo TLS phía ngoài.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish ./
# Thư mục lưu avatar upload (mount volume ở compose để không mất khi redeploy).
RUN mkdir -p /app/wwwroot/uploads
ENTRYPOINT ["dotnet", "MeUp.Api.dll"]
