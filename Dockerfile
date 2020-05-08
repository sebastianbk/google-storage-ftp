FROM microsoft/dotnet:2.2-sdk
WORKDIR /app

# Expose ports
EXPOSE 20/tcp
EXPOSE 20/udp
EXPOSE 21/tcp
EXPOSE 21/udp
EXPOSE 989/tcp
EXPOSE 989/udp
EXPOSE 990/tcp
EXPOSE 990/udp
EXPOSE 10000/tcp
EXPOSE 10000/udp
EXPOSE 10001/tcp
EXPOSE 10001/udp
EXPOSE 10002/tcp
EXPOSE 10002/udp
EXPOSE 10003/tcp
EXPOSE 10003/udp
EXPOSE 10004/tcp
EXPOSE 10004/udp
EXPOSE 10005/tcp
EXPOSE 10005/udp
EXPOSE 10006/tcp
EXPOSE 10006/udp
EXPOSE 10007/tcp
EXPOSE 10007/udp
EXPOSE 10008/tcp
EXPOSE 10008/udp
EXPOSE 10009/tcp
EXPOSE 10009/udp

# Copy csproj and restore as distinct layers
COPY src/GoogleStorageFtp.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY src/. ./
RUN dotnet publish -c Release -o out

# Copy PFX and service account files
COPY ftp.pfx ./

# Run the app
ENTRYPOINT ["dotnet", "out/GoogleStorageFtp.dll"]
