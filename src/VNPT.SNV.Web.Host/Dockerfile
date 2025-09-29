FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["src/VNPT.SNV.Web.Host/VNPT.SNV.Web.Host.csproj", "src/VNPT.SNV.Web.Host/"]
COPY ["src/VNPT.SNV.Web.Core/VNPT.SNV.Web.Core.csproj", "src/VNPT.SNV.Web.Core/"]
COPY ["src/VNPT.SNV.Application/VNPT.SNV.Application.csproj", "src/VNPT.SNV.Application/"]
COPY ["src/VNPT.SNV.Core/VNPT.SNV.Core.csproj", "src/VNPT.SNV.Core/"]
COPY ["src/VNPT.SNV.EntityFrameworkCore/VNPT.SNV.EntityFrameworkCore.csproj", "src/VNPT.SNV.EntityFrameworkCore/"]
WORKDIR "/src/src/VNPT.SNV.Web.Host"
RUN dotnet restore 

WORKDIR /src
COPY ["src/VNPT.SNV.Web.Host", "src/VNPT.SNV.Web.Host"]
COPY ["src/VNPT.SNV.Web.Core", "src/VNPT.SNV.Web.Core"]
COPY ["src/VNPT.SNV.Application", "src/VNPT.SNV.Application"]
COPY ["src/VNPT.SNV.Core", "src/VNPT.SNV.Core"]
COPY ["src/VNPT.SNV.EntityFrameworkCore", "src/VNPT.SNV.EntityFrameworkCore"]
WORKDIR "/src/src/VNPT.SNV.Web.Host"
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "VNPT.SNV.Web.Host.dll"]
