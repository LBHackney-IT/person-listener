# Contributing

## Dependencies

- .NET SDK for `net10.0`, matching the `.csproj` target framework.
- Docker, for the Compose services below.
- A GitHub packages token in `LBHPACKAGESTOKEN`. `NuGet.Config` uses it to restore Hackney packages. Compose passes it as a build arg.

## Local DynamoDB

Tests expect DynamoDB on `localhost:8000`.

Compose service `dynamodb-database` (`amazon/dynamodb-local`) publishes that port:

```sh
docker-compose up -d dynamodb-database
```

The same port can be started without Compose. Replace the volume path with a local directory:

```sh
docker run --name dynamodb-local -p 8000:8000 -v <PUT YOUR LOCAL PATH HERE>:/data/ amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb -dbPath /data
```

`make restart-db` stops and recreates the `dynamodb-database` container.

## Run

`make serve` builds the `person-listener` image and starts it. The service publishes port `3000`.

```sh
make build && make serve
```

`make shell` opens a shell in the `person-listener` container.

## Test

`make test` starts `dynamodb-database`, builds `person-listener-test`, and runs that service.

```sh
make test
```

From the repository root, with DynamoDB already listening on port `8000`:

```sh
dotnet test
```

`make lint` installs or updates `dotnet-format` and runs `dotnet format`.
