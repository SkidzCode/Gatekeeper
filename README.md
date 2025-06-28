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

GateKeeper uses the .NET User Secrets feature to manage sensitive configuration data securely during development. To simplify the setup of these secrets, a PowerShell script is provided.

**Automated Setup using `setup-user-secrets.ps1`:**

1.  **Locate the script:** Find `setup-user-secrets.ps1` in the root of the repository.
2.  **Create `secrets.template.json`:**
    *   In the root of the repository, create a file named `secrets.template.json`.
    *   Copy the structure from `GateKeeper.Server/UserSecret.Example` into `secrets.template.json`.
    *   Alternatively, a `secrets.template.json` is already provided in the root directory. You can modify this file.
    *   Fill in your specific secret values in this file. The `KeyManagement:MasterKey` will be generated and populated by the script.
3.  **Run the script:**
    *   Open a PowerShell terminal.
    *   Navigate to the root directory of the GateKeeper project.
    *   Execute the script: `.\setup-user-secrets.ps1`
    *   The script will guide you through the process, initialize user secrets in `GateKeeper.Server` (if not already done), generate the `MasterKey`, and set all secrets based on your `secrets.template.json` file.

**Secret Configuration Structure (for `secrets.template.json`):**

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
      * `MasterKey`: "" (This will be auto-generated by the script)
  * **Resources**:
      * `Path`: "./" (Path for resource files, adjust if necessary)

For more details on User Secrets and environment variables, please consult the [Configuration section in GetStarted.md](GetStarted.md#configuration).

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

## Plugin System

GateKeeper features a plugin system that allows for extending the application's functionality with new features, including new pages and navigation items in the frontend portal. This system is designed to be modular, allowing developers to create self-contained plugins for both backend and frontend capabilities.

### Overview

The plugin architecture relies on:

*   **Backend Discovery:** .NET class libraries implementing the `IPlugin` interface (from `GateKeeper.Plugin.Abstractions`) are discovered by the server at startup. Each plugin can register its own services and provide metadata.
*   **API Manifest:** The `PluginsController` (`/api/plugins/manifests`) exposes a list of active plugins and their metadata (name, version, frontend module paths, route paths, navigation labels) to the frontend.
*   **Frontend Dynamic Loading:**
    *   The Angular frontend's `PluginLoaderService` fetches this manifest on application initialization.
    *   A build-time script (`gatekeeper.client/esbuild-plugins.cjs`) scans the `gatekeeper.client/src/app/plugins/` directory for Angular modules. It generates a map that allows Angular to lazy-load these modules.
    *   The main Angular router dynamically adds routes for active plugins based on the manifest and the build-time module map.
    *   Navigation elements (e.g., in the portal sidebar) are also dynamically generated based on the plugin manifest.

### Creating a New Plugin

Here's a high-level guide to creating a new plugin (e.g., "MyExamplePlugin").

**1. Backend C# Plugin Project:**

*   **Create Project:** Create a new .NET Class Library project (e.g., `GateKeeper.Plugin.MyExamplePlugin`). Target `net8.0` or a compatible framework.
*   **Add Reference:** Reference the `GateKeeper.Plugin.Abstractions` project.
*   **Implement `IPlugin`:** Create a class (e.g., `MyExamplePlugin.cs`) that implements `GateKeeper.Plugin.Abstractions.IPlugin`.
    *   **Key Properties to Define:**
        *   `Name`: (string) Display name of the plugin.
        *   `Version`: (string) Plugin version.
        *   `Description`: (string) Short description.
        *   `DefaultRoutePath`: (string) The base path segment for this plugin's routes under `/portal/` (e.g., `"myexample"` would result in `/portal/myexample`).
        *   `AngularModuleName`: (string) The exported class name of your plugin's main Angular module (e.g., `"MyExamplePluginModule"`).
        *   `AngularModulePath`: (string) The path to your plugin's main Angular module file, relative to `gatekeeper.client/src/app/` and without the `.ts` extension (e.g., `"plugins/myexample/myexample.module"`).
        *   `NavigationLabel`: (string) Text for the link in the navigation menu.
        *   `RequiredRole`: (string, optional) Role required to access this plugin.
    *   **`ConfigureServices` Method:** Implement this method to register any backend services specific to your plugin using the provided `IServiceCollection`.
*   **Add Server Reference:** Add a project reference from `GateKeeper.Server.csproj` to your new plugin project (`GateKeeper.Plugin.MyExamplePlugin.csproj`).

**2. Frontend Angular Module:**

*   **Create Directory:** In the `gatekeeper.client` project, create a new directory for your plugin's frontend code: `src/app/plugins/myexample/`.
*   **Angular Module (`myexample.module.ts`):**
    *   Create your main Angular module file (e.g., `src/app/plugins/myexample/myexample.module.ts`).
    *   Define and export the module class (e.g., `MyExamplePluginModule`).
    *   Import `CommonModule`, a routing module for your plugin, and declare/import any components specific to this plugin.
*   **Component(s) (`myexample.component.ts`, `.html`, `.scss`):**
    *   Create the Angular components that make up your plugin's UI.
    *   Consider using standalone components if that aligns with the project's current practices.
*   **Routing Module (`myexample-routing.module.ts`):**
    *   Create a routing module for routes *within* your plugin (e.g., `src/app/plugins/myexample/myexample-routing.module.ts`).
    *   Define child routes for your plugin components (e.g., `path: '', component: MyExampleComponent`).
*   **Services (Optional):** If your plugin's frontend needs to call APIs, create Angular services within its directory (e.g., `src/app/plugins/myexample/services/`). These services can be `providedIn: 'root'` or scoped to your plugin's module. They can call general application APIs or APIs specific to your plugin (defined in its backend C# project).

**3. Build and Verify:**

*   **Rebuild Solution:** Ensure both backend and frontend projects are rebuilt.
*   **Run Application:** Start both the backend server and the Angular development server.
*   **Check:**
    *   Backend server logs for discovery of "MyExamplePlugin".
    *   The `/api/plugins/manifests` endpoint to see your plugin listed.
    *   The portal navigation menu for the `NavigationLabel`.
    *   Navigate to your plugin's route (e.g., `/portal/myexample`) to see its UI.
    *   Browser developer tools (Network tab) to confirm lazy loading of your plugin's JavaScript module when first accessed.

This plugin system allows for extending GateKeeper with new, self-contained features while maintaining a decoupled architecture.

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