# GateKeeper

GateKeeper is a robust and scalable application designed to manage and secure access to your resources. This project leverages .NET 8 and MariaDB 10 to deliver high performance and reliability.

## Getting Started

### Prerequisites

Ensure you have the following installed:
- .NET 8 SDK
- Node.js and npm
- MariaDB 10

### Installation

Follow these steps to set up the project:

#### NPM Commands

1. Navigate to the `gatekeeper.client` directory.
2. Execute the following commands to install the latest npm packages:

#### Database Setup

This project uses a MariaDB 10 database. To set it up:

1. Run all the table scripts.
2. Execute the stored procedures.
3. Run the scripts located in the `script` folder.

#### Appsettings Configuration

Add the following items to your `appsettings.json` file:

```json
{
  "EmailSettings": {
    "SmtpHost": "server.url.or.ip",
    "Port": "123",
    "UserName": "email@email.com",
    "Password": "password",
    "FromName": "From Name",
    "UseSsl": "true"
  },
  "JwtConfig": {
    "Secret": "SuperSecretKey",
    "Issuer": "Issuer",
    "Audience": "Audience",
    "ExpirationMinutes": 15
  },
  "DatabaseConfig": {
    "Server": "server.url.or.ip",
    "Database": "databaseName",
    "User": "api_user",
    "Password": "Password"
  }
}
```

## Contributing

We welcome contributions! Please read our [Contributing Guidelines](CONTRIBUTING.md) for more information.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any inquiries or issues, please contact us.