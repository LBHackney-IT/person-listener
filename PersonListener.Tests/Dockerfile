FROM mcr.microsoft.com/dotnet/sdk:8.0

ENV DOTNET_CLI_TELEMETRY_OPTOUT='true'

ENV DynamoDb_LocalMode='true'
ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN

WORKDIR /app

COPY ./PersonListener.sln ./
COPY ./PersonListener/PersonListener.csproj ./PersonListener/
COPY ./PersonListener.Tests/PersonListener.Tests.csproj ./PersonListener.Tests/
COPY /NuGet.Config /root/.nuget/NuGet/NuGet.Config

RUN dotnet restore ./PersonListener/PersonListener.csproj
RUN dotnet restore ./PersonListener.Tests/PersonListener.Tests.csproj

COPY . .

RUN dotnet build -c Release -o out PersonListener/PersonListener.csproj
RUN dotnet build -c debug -o out PersonListener.Tests/PersonListener.Tests.csproj

ENTRYPOINT ["dotnet", "test", "--collect", "XPlat Code Coverage;Format=opencover", "--results-directory", "./coverage"]
