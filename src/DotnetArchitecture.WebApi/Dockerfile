# syntax=docker/dockerfile:1

# ==============================================================================
# 1. AŞAMA: DERLEME (BUILD STAGE)
# ==============================================================================
# .NET 10 SDK imajı kullanarak bağımlılıkları çözer, derler ve yayımlar.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Docker katman önbelleğinden (layer cache) en iyi şekilde yararlanmak için
# kaynak kodlardan ÖNCE sadece .csproj proje dosyalarını kopyalıyoruz.
COPY ["src/DotnetArchitecture.Domain/DotnetArchitecture.Domain.csproj", "src/DotnetArchitecture.Domain/"]
COPY ["src/DotnetArchitecture.Application/DotnetArchitecture.Application.csproj", "src/DotnetArchitecture.Application/"]
COPY ["src/DotnetArchitecture.Persistence/DotnetArchitecture.Persistence.csproj", "src/DotnetArchitecture.Persistence/"]
COPY ["src/DotnetArchitecture.WebApi/DotnetArchitecture.WebApi.csproj", "src/DotnetArchitecture.WebApi/"]

# Bağımlılıkları geri yükle (Restore)
RUN dotnet restore "src/DotnetArchitecture.WebApi/DotnetArchitecture.WebApi.csproj"

# Kalan tüm kaynak kodları kopyala
COPY src/ src/

# WebApi projesini Release modunda derle ve /app/publish dizinine paketle (Publish)
WORKDIR "/src/src/DotnetArchitecture.WebApi"
RUN dotnet publish "DotnetArchitecture.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# 2. AŞAMA: ÇALIŞMA ZAMANI (RUNTIME STAGE)
# ==============================================================================
# Üretim ortamında SDK'ya ihtiyaç yoktur; yalnızca hafif ve optimize edilmiş
# ASP.NET Core Runtime imajı kullanılır (~150MB).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080

# Derleme aşamasında üretilen yayın çıktısını kopyala
COPY --from=build /app/publish .

# Güvenlik En İyi Pratiği: Konteyneri root yetkisiyle değil,
# dahili 'app' kullanıcısıyla çalıştır (Least Privilege Principle)
USER $APP_UID

ENTRYPOINT ["dotnet", "DotnetArchitecture.WebApi.dll"]
