# GateKeeper

GateKeeper is a robust and scalable application designed to manage and secure access to your resources. It provides a comprehensive suite of tools for user management, authentication, resource control, and secure logging, built with modern technologies like .NET 8 and Angular.

## Core Features

  * **User Management:**
      * Secure user registration with email verification and password validation.
      * JWT-based authentication with refresh tokens for stateless sessions.
      * Role-Based Access Control (RBAC) with predefined and custom roles.
      * Admin area for comprehensive user and role management.
  * **Resource & Settings Management:**
      * Control access to various resource types (Files, APIs, Tools).
      * Centralized settings management for application defaults.
      * Session monitoring and revocation capabilities.
  * **Security:**
      * Strong password hashing (PBKDF2 + Salt).
      * Scheduled key rotation and secure key storage.
      * Encryption at rest and in transit (HTTPS).
      * Tamper-proof audit logging compliant with HIPAA & GDPR standards.
  * **Notifications:**
      * Built-in system for sending alerts and messages to users via email.
      * Manageable notification templates.
  * **Scalability & Performance:**
      * Microservices-friendly architecture.
      * Optimized frontend and backend with caching and asynchronous operations.
  * **Developer Friendly:**
      * Swagger for interactive API documentation.
      * Hangfire dashboard for background task monitoring.

## Technologies Used

  * **Backend:** .NET 8, ASP.NET Core Web API, Entity Framework Core
  * **Frontend:** Angular, HttpClientModule
  * **Database:** MariaDB 10
  * **Security:** JWT, HTTPS, PBKDF2
  * **Development Tools:** Visual Studio 2022, Node.js/npm, .NET 8 SDK

## Prerequisites

Ensure your development environment meets the following requirements:

  * .NET 8 SDK
  * Node.js and npm
  * MariaDB 10
  * Git
    For detailed hardware and optional tool recommendations, see the [Getting Started Guide](https://www.google.com/search?q=GetStarted.md).

## Getting Started

### 1\. Clone the Repository

```bash
git clone https://github.com/yourusername/gatekeeper.git
cd gatekeeper
```

### 2\. Frontend Setup

```bash
cd gatekeeper.client
npm install
```

### 3\. Backend Setup

```bash
cd ../GateKeeper.Server
dotnet restore
```

### 4\. Database Setup

GateKeeper uses MariaDB 10. You'll need to:

1.  Ensure your MariaDB server is running.
2.  Create the database (e.g., `gatekeeper_db`).
3.  Execute the SQL scripts located in the `SQL/Scripts` and `SQL/Tables` folders. Start with `Init.sql` (edit user credentials first) and `HangfireDatabase.sql` (update password). Then run scripts for tables, stored procedures, and seed data.
    For detailed instructions, refer to the [Database Setup section in GetStarted.md](https://www.google.com/search?q=GetStarted.md%23database-setup).

## Configuration

GateKeeper uses the .NET User Secrets feature to manage sensitive configuration data securely during development.

**Important:** Initialize user secrets for the `GateKeeper.Server` project:

```bash
cd GateKeeper.Server
dotnet user-secrets init
```

Then, set your secrets. Below is the structure for your `secrets.json` file. Update the values accordingly.

**Current Secret Configuration Structure:**

  * **EmailSettings**:
      * `SmtpServer`: "SMTP server address"
      * `Port`: "Port number"
      * `UserName`: "SMTP Username"
      * `Password`: "SMTP Password" 
      * `FromAddress`: "From Address"
  * **Jwt**:
      * `Key`: "(Must be a strong, long, and unique key)" 
      * `Issuer`: "(e.g., your application's domain)" 
      * `Audience`: "(e.g., your application's domain or a specific audience identifier)" 
      * `TokenValidityInMinutes`: 15
      * `RefreshTokenValidityInDays`: 30
  * **DatabaseConfig**:
      * `ConnectionString`: "(Connection string to your MySQL database)"
  * **ConnectionStrings**:
      * `HangfireConnection`: "(Connection string to your Hangfire database)"
  * **KeyManagement**:
      * `MasterKey`: "(Ensure this is a securely generated AES key)"
  * **Resources**:
      * `Path`: "./" (Path for resource files, adjust if necessary)

**To set these secrets using the command line:**
In visual studion 2022 you can right click the project and select "Manage User Secrets" to open the secrets.json file directly, or you can use the command line to set each secret individually.
You can also use command line to set each secret individually. Open a terminal and navigate to the `GateKeeper.Server` directory, then use the following commands:

```bash
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
```

Repeat for all necessary settings.

For more details on User Secrets, environment variables, and generating an AES MasterKey using the `generatekey.ps1` script, please consult the [Configuration section in GetStarted.md](https://www.google.com/search?q=GetStarted.md%23configuration).

## Running the Application

### Start Backend Server

```bash
cd GateKeeper.Server
dotnet run
```

The backend API will typically be available at `https://localhost:5001`.

### Start Frontend Application

In a new terminal:

```bash
cd gatekeeper.client
npm start
```

The frontend will typically be available at `http://localhost:4200`.

## Testing

Both frontend and backend include unit tests.

  * **Frontend Tests:** Navigate to `gatekeeper.client` and run `npm test`.
  * **Backend Tests:** Navigate to `GateKeeper.Server` (or the test project directory) and run `dotnet test`.
    Refer to [GetStarted.md](https://www.google.com/search?q=GetStarted.md%23testing) for more details on running tests.

## Error Handling

The GateKeeper API employs a centralized error handling middleware to provide consistent error responses. When an error occurs, the API will return a JSON response in a standard format.

### Standard Error Response Format

The `ErrorResponse` model includes the following properties:

*   `StatusCode` (int): The HTTP status code for the error (e.g., 400, 404, 500).
*   `Message` (string): A human-readable message summarizing the error. For specific errors like validation or business rule violations, this message will provide more context.
*   `Details` (string, optional): Contains more detailed error information, such as a stack trace. This field is ONLY populated in development environments for debugging purposes and will be `null` in production environments to avoid exposing sensitive information.
*   `TraceId` (string): A unique identifier for the request. This ID is logged by the server and can be used to correlate server-side logs with the specific error encountered by the client. Please include this `TraceId` when reporting issues.

**Example JSON Error Response:**

```json
{
  "StatusCode": 500,
  "Message": "An unexpected internal server error occurred. Please try again later.",
  "Details": null, // Or a detailed error string (e.g., stack trace) in development
  "TraceId": "0HM123456789ABCDEF"
}
```

### Common HTTP Status Codes

The API uses standard HTTP status codes to indicate the success or failure of an API request. Here are some common ones you might encounter:

*   **`400 Bad Request`**: This can indicate several issues:
    *   **Validation Errors**: Input data failed validation (e.g., missing required fields, invalid data format). The `Message` field will often detail which fields are problematic.
    *   **Business Rule Violations**: The request violated a specific business rule (e.g., trying to create a duplicate entry where it's not allowed). The `Message` will describe the rule violation.
*   **`401 Unauthorized`**: Authentication is required to access the resource, and the request either lacked authentication credentials or the provided credentials were invalid.
*   **`403 Forbidden`**: The authenticated user does not have the necessary permissions to perform the requested action on the resource.
*   **`404 Not Found`**: The requested resource (e.g., a specific user, role, or setting) could not be found on the server.
*   **`500 Internal Server Error`**: An unexpected error occurred on the server that prevented it from fulfilling the request. The `Message` will typically be generic. Use the `TraceId` to help administrators locate the corresponding server logs for more details.

### Custom Exceptions

The centralized error handler is designed to catch specific custom exceptions and map them to appropriate HTTP status codes and user-friendly messages. These include:

*   `ValidationException`: Typically results in a `400 Bad Request` with a message detailing the validation failure.
*   `ResourceNotFoundException`: Results in a `404 Not Found` with a message indicating the resource was not found.
*   `BusinessRuleException`: Typically results in a `400 Bad Request` (or sometimes `409 Conflict`) with a message explaining the business rule that was violated.

This approach ensures that clients receive predictable and informative error responses, aiding in debugging and integration.

## Documentation & Project Structure

  * **[GetStarted.md](https://www.google.com/search?q=GetStarted.md):** Comprehensive guide for installation, detailed configuration, and setup.
  * **[CONTRIBUTING.md](https://www.google.com/search?q=CONTRIBUTING.md):** Guidelines for contributing to the project.
  * **Frontend (`gatekeeper.client`):** Angular application.
  * **Backend (`GateKeeper.Server`):** ASP.NET Core Web API.
  * **SQL Scripts (`SQL/`):** Database schema, stored procedures, and seed data.

## Contributing

Contributions are welcome\! Please read [CONTRIBUTING.md](https://www.google.com/search?q=CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.