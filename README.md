# Person Listener

Person Listener is a .NET 10 AWS Lambda triggered by the person SQS queue. It reads each message as an entity event and updates the matching person record in the DynamoDB `Persons` table. It copies tenure details onto the person, removes a tenure when a household member leaves, and sets the tenure payment reference when an account is created. It does not publish a further event.

`TenureCreatedEvent` is ignored.

## Events

| Event | Source | What it does |
|---|---|---|
| `PersonAddedToTenureEvent` | Person SQS queue (`/sqs-queue/{stage}/person/arn`) | Loads the tenure from the Tenure API, finds the household member added in the event, and adds or updates that person's tenure details and person type. |
| `PersonRemovedFromTenureEvent` | Person SQS queue | Removes that tenure from the person and rebuilds the person's types from the tenures that remain. |
| `TenureUpdatedEvent` | Person SQS queue | Loads the tenure and copies its address, dates, type, references, and payment reference onto each household member's person record. |
| `AccountCreatedEvent` | Person SQS queue | Loads the account from the Account API, then the tenure it points at, and writes the account payment reference onto each household member's tenure on the person record. |
| `TenureCreatedEvent` | Person SQS queue | No update. The message is logged and skipped. |

## Environment variables

| Variable | Where it is set | Purpose |
|---|---|---|
| `ENVIRONMENT` | SSM `/housing-tl/{stage}/aspnetcore-environment` | Selects `appsettings.{ENVIRONMENT}.json`. Local Docker uses `LocalDevelopment`. |
| `TenureApiUrl` | SSM `/housing-tl/{stage}/tenure-api-url` | Base URL for the Tenure API. |
| `TenureApiToken` | SSM `/housing-tl/{stage}/tenure-api-token` | Token sent to the Tenure API. |
| `AccountApiUrl` | SSM `/housing-finance/{stage}/account-api-url` | Base URL for the Account API. |
| `AccountApiToken` | SSM `/housing-tl/{stage}/account-api-token` | Token sent to the Account API. |
| `DynamoDb_LocalMode` | `appsettings.LocalDevelopment.json` and Docker Compose | Points the DynamoDB client at a local instance when `true`. |
| `DynamoDb_LocalServiceUrl` | `appsettings.LocalDevelopment.json` and Docker Compose | Local DynamoDB URL. Tests use `http://localhost:8000`. Compose uses `http://dynamodb-database:8000`. |

## HTTP APIs

| API | Base URL | Call | Purpose |
|---|---|---|---|
| Tenure API | `TenureApiUrl` | `GET {base}/tenures/{id}` | Load the tenure when a person is added or removed, when a tenure is updated, and after an account is created. |
| Account API | `AccountApiUrl` | `GET {base}/accounts/{id}` | Load the account for `AccountCreatedEvent`, including its payment reference and target tenure. |

Person records are stored in DynamoDB. The Lambda does not call SNS.

## Stack

- Target framework `net10.0`. Lambda runtime `dotnet10`, region `eu-west-2`.
- Triggered by SQS. Deployed with the Serverless Framework. Related infrastructure is Terraform.
- X-Ray tracing is enabled on the Lambda. The function runs in a VPC.
- Storage is the DynamoDB `Persons` table (`id` hash key).

## Contacts

Housing product team, mmh-project-team@hackney.gov.uk.

Build, run, and test steps are in [CONTRIBUTION.md](CONTRIBUTION.md).
