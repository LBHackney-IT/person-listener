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
See setup instructions within the setup readme: [link](https://github.com/LBHackney-IT/person-listener/blob/master/docs/Setup.md).

## Contributing
See contributing instructions within the contribution readme: [link](https://github.com/LBHackney-IT/person-listener/blob/master/docs/Contributing.md).


## Ownership
**Housing Products Team**, [@LBHackney-IT/teams/housing-products](https://github.com/orgs/LBHackney-IT/teams/housing-products).
