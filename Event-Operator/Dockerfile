FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim
ARG TARGETARCH
EXPOSE 80/TCP
WORKDIR /app/
COPY $TARGETARCH ./

ENTRYPOINT ["./Event-Operator"]