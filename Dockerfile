FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /app

#Install rcon-cli
RUN wget -O /usr/bin/rcon-cli https://github.com/itzg/rcon-cli/releases/download/1.3/rcon-cli_linux_amd64
RUN chmod +x /usr/bin/rcon-cli

#Install kubectl
RUN wget -O /usr/bin/kubectl https://storage.googleapis.com/kubernetes-release/release/$(curl -s https://storage.googleapis.com/kubernetes-release/release/stable.txt)/bin/linux/amd64/kubectl
RUN chmod +x /usr/bin/kubectl 

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . ./
RUN dotnet publish -c release -o out


FROM microsoft/aspnetcore:2.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
COPY --from=build /usr/bin/rcon-cli /usr/bin/
COPY --from=build /usr/bin/kubectl /usr/bin/
ENTRYPOINT ["dotnet", "MineCraftMonitor.dll"]
