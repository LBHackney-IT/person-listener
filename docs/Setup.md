# Setup

## Dependencies
1. Install [Docker][docker-download].
2. Install [AWS CLI][AWS-CLI].
3. Install the dotnet SDK and dotnet Runtime version matching those specified in `.csproj` files.
4. Install `git` versioning software. Intall it with `git bash` if you're on Windows.
5. Configure your git credentials with personal access token (PAT) from Github. This token needs to be SSO authorized within Github settings.

## Open the project
1. Clone this repository using `git clone` command that Github provides.
2. Open it in your IDE _(Integrated development environment)_.
3. Configure any needed environment variables within an `.env` file or within your environment.

## Integrated development environment
There are many options available. What fits you best will depend on your Operating System, you preference on Open Source, and your preference convenience in terms of reducing configuration needed.

Common options used in Hackney:
1. Visual Studio Code _(great & common-choice IDE for both front-end and back-end, requires installing a few pluggins, and doing minor configuration)_
2. Visual Studio _(only available on Mac and Windows, best on Windows. Nearly everything C# works w/o the need for extra configuration apart the setup wizard prompt it requires you to go through)_
3. JetBrains Rider _(offers a bunch of convenience, available on all OS'es, however, requires paid subscription)_

## Running project locally
So long as you have the correct dotnet SDK and runtime installed, there shouldn't be any issue.
If you don't have those installed, you can still use docker container to run the application.

The launching of the application can be done in multiple ways, either through the IDE user interface buttons,
or via dotnet command-line-interface by running `dotnet run`.

**Note**
When running locally the appropriate database connection details and other environment variables like Nuget token are still needed.

## DynamoDb
To use a local instance of DynamoDb, this will need to be installed. This is most easily done using [Docker](https://www.docker.com/products/docker-desktop).
Run the following command, specifying the local path where you want the container's shared volume to be stored.
```
docker run --name dynamodb-local -p 8000:8000 -v <PUT YOUR LOCAL PATH HERE>:/data/ amazon/dynamodb-local -jar DynamoDBLocal.jar -sharedDb -dbPath /data
```

If you would like to see what is in your local DynamoDb instance using a simple gui, then [this admin tool](https://github.com/aaronshaf/dynamodb-admin) can do that.

The application can also be served locally using docker:
1.  Add you security credentials to AWS CLI.
```sh
$ aws configure
```
<!-- The log in to ECR makes no sense to me. If anyone's done this setup, please elaborate. -->
2. Log into AWS ECR.
```sh
$ aws ecr get-login --no-include-email
```
3. Build and serve the application. It will be available in the port 3000.
```sh
$ make build && make serve
```

## Run the tests locally
Run the tests within the Docker container:
```sh
$ make test
```

Alternatively, run the tests locally:
```sh
dotnet test
```
However, you may still need to launch the database container separately if it's used by the tests.
