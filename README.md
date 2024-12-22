# GateKeeper

### NPM commands

After downloading you ahve to issue a few commands to get the latest npm packages

1. change to the "gatekeeper.client" directory
2. Issue the following command:
  A. `Remove-Item -Recurse -Force node_modules, package-lock.json`
  B. `npm install` 

### Appsetting.json

You will need to add the following itmes to the appsettings.

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