FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
WORKDIR /build
COPY src /build
RUN dotnet restore Raspi-LED-Matrix.csproj
RUN dotnet publish -c release -r linux-arm -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim-arm32v7
WORKDIR /app
COPY --from=builder /app .

ENTRYPOINT ["./Raspi-LED-Matrix"]