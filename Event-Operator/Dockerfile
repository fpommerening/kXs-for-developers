FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal
ARG TARGETARCH
EXPOSE 80/TCP
WORKDIR /app/
COPY $TARGETARCH ./

ENTRYPOINT ["./Event-Operator"]