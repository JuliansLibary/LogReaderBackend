FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
COPY /bin/[Rr]elease/net6.0 ./
ENV ASPNETCORE_URLS=http://+:4001
EXPOSE 4001
ENTRYPOINT ["dotnet", "LogReaderBackend.dll"]
