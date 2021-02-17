FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
WORKDIR /build
COPY src /build
RUN dotnet restore Raspi-LED-Matrix.csproj
RUN dotnet publish -c release -r linux-arm -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-bionic-arm32v7
WORKDIR /app
COPY --from=builder /app .

ENTRYPOINT ["./Raspi-LED-Matrix"]