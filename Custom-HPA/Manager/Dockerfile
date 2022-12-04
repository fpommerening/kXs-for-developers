FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal
EXPOSE 5001/TCP
EXPOSE 5002/TCP
WORKDIR /app/
COPY out ./

ENTRYPOINT ["./Manager"]