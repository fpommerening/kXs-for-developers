FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
WORKDIR /build
COPY src /build
RUN dotnet restore Event-Operator.csproj
RUN dotnet publish -c release -r linux-x64 -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=builder /app .

ENTRYPOINT ["./Event-Operator"]