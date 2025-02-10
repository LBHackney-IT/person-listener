# Person Listener

Listener application implementing an AWS function to receive messages that will result in additional processing within the person domain.

Whenever a person record gets assigned / removed to any given tenancy agreement record, this ends up firing off an event of a
corresponding type. This listener picks this event up, and links up the tenancy agreement record by attaching / removing it to/from
the person record.

Tenancy Agreement DynamoDB table document details are being duplicated within the Person DynamoDB document record. This creates a
constraint where the Tenancy data needs to be synced between the two tables. This appears in the form of the above tenanancy
agreement being attached / removed as partial copy of the full tenancy agreement table record. However, this also results in
person DynamoDB record needing to be updated whenever there are any changes to the tenancy details, or payment reference number.

Person listener does the above-described table data copying plumbing behind the scenes to ensure that these linked up tables are in sync.

## Stack

- .NET Core as a web framework.
- xUnit as a test framework.


## Setup

1. Install [Docker][docker-download].
2. Install [AWS CLI][AWS-CLI].
3. Clone this repository.
4. Rename the initial template.
5. Open it in your IDE.

## Contributing
See contributing instructions within the following readme: [link](https://github.com/LBHackney-IT/person-listener/blob/master/docs/Contributing.md).

## Development

To serve the application, run it using your IDE of choice, we use Visual Studio CE and JetBrains Rider on Mac.

**Note**
When running locally the appropriate database conneciton details are still needed.

### DynamoDb
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
2. Log into AWS ECR.
```sh
$ aws ecr get-login --no-include-email
```
3. Build and serve the application. It will be available in the port 3000.
```sh
$ make build && make serve
```



## Contacts

### Active Maintainers

- **Selwyn Preston**, Lead Developer at London Borough of Hackney (selwyn.preston@hackney.gov.uk)
- **Mirela Georgieva**, Lead Developer at London Borough of Hackney (mirela.georgieva@hackney.gov.uk)
- **Matt Keyworth**, Lead Developer at London Borough of Hackney (matthew.keyworth@hackney.gov.uk)

### Other Contacts

- **Rashmi Shetty**, Product Owner at London Borough of Hackney (rashmi.shetty@hackney.gov.uk)

[docker-download]: https://www.docker.com/products/docker-desktop
[AWS-CLI]: https://aws.amazon.com/cli/
