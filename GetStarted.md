# GateKeeper: Getting Started Guide

Welcome to **GateKeeper**, a robust and scalable application designed to manage and secure access to your resources. This guide will walk you through the prerequisites, installation process, configuration, and other essential steps to get you up and running with GateKeeper.

## Table of Contents

1. [Introduction](#introduction)
2. [Prerequisites](#prerequisites)
3. [Installation](#installation)
    - [Cloning the Repository](#cloning-the-repository)
    - [Frontend Setup](#frontend-setup)
    - [Backend Setup](#backend-setup)
    - [Database Setup](#database-setup)
4. [Configuration](#configuration)
    - [Using Manager User Secrets](#using-manager-user-secrets)
    - [Environment Variables](#environment-variables)
    - [Automated Secret Setup](#automated-secret-setup)
5. [Running the Application](#running-the-application)
    - [Starting the Backend Server](#starting-the-backend-server)
    - [Starting the Frontend Application](#starting-the-frontend-application)
6. [Testing](#testing)
    - [Running Unit Tests](#running-unit-tests)
    - [Running Integration Tests](#running-integration-tests)
7. [Deployment](#deployment)
    - [Production Considerations](#production-considerations)
    - [Continuous Integration/Continuous Deployment (CI/CD)](#continuous-integrationcontinuous-deployment-cicd)
8. [Contributing](#contributing)
9. [License](#license)
10. [Contact](#contact)

---

## Introduction

**GateKeeper** is built using modern technologies to ensure high performance, reliability, and security. Leveraging **.NET 8** for the backend and **MariaDB 10** for the database, GateKeeper provides a solid foundation for managing user access and resources effectively.

---

## Prerequisites

Before setting up GateKeeper, ensure that your development environment meets the following requirements:

### Software Requirements

- **.NET 8 SDK**
  - [Download .NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js and npm**
  - [Download Node.js](https://nodejs.org/)
  - npm is included with Node.js
- **MariaDB 10**
  - [Download MariaDB](https://mariadb.org/download/)
- **Git**
  - [Download Git](https://git-scm.com/downloads)

### Hardware Requirements

- **Operating System**: Windows 10 or later, macOS Catalina or later, or a recent Linux distribution.
- **Memory**: Minimum 8 GB RAM recommended.
- **Disk Space**: At least 10 GB of free space.

### Optional Tools

- **Visual Studio 2022 or later**
  - [Download Visual Studio](https://visualstudio.microsoft.com/downloads/)
- **Postman** (for API testing)
  - [Download Postman](https://www.postman.com/downloads/)
- **Docker** (for containerization)
  - [Download Docker](https://www.docker.com/get-started)

---

## Installation

Follow these steps to set up the GateKeeper project on your local machine.

### Cloning the Repository

1. **Clone the Repository**

   Open your terminal or command prompt and execute the following command to clone the GateKeeper repository:

   ```bash
   git clone https://github.com/yourusername/gatekeeper.git
   ```

2. **Navigate to the Project Directory**

   ```bash
   cd gatekeeper
   ```

### Frontend Setup

1. **Navigate to the Frontend Directory**

   ```bash
   cd gatekeeper.client
   ```

2. **Install npm Packages**

   Install the necessary frontend dependencies using npm:

   ```bash
   npm install
   ```

3. **Build the Frontend Application**

   Optionally, you can build the frontend for production:

   ```bash
   npm run build
   ```

### Backend Setup

1. **Navigate to the Backend Directory**

   ```bash
   cd ../GateKeeper.Server
   ```

2. **Restore .NET Packages**

   Restore the backend dependencies using the .NET CLI:

   ```bash
   dotnet restore
   ```

### Database Setup

GateKeeper uses a **MariaDB 10** database to store and manage data. Follow these steps to set up the database:

1. **Start MariaDB Server**

   Ensure that your MariaDB server is running. You can start it using the command line or through your preferred database management tool.

2. **Create the Database**

   Log into MariaDB and create a new database for GateKeeper:

   ```sql
   CREATE DATABASE gatekeeper_db;
   ```

3. **Initialize Database Scripts**

   The `scripts` folder contains multiple SQL scripts necessary for setting up the database. It includes:

   - **Init.sql**: Sets up the API user with the minimal required permissions. **Important:** Before running this script, you need to edit it to configure the API user's credentials and permissions as per your environment.
   - **Hangfiredb.sql**: Sets up the Hangfire database. **Important:** Update the password in this script to secure your Hangfire setup.
   - **Table Scripts**: Multiple scripts located in the `scripts/tables` directory to create the necessary tables.
   - **Stored Procedure Scripts**: Multiple scripts located in the `scripts/stored_procedures` directory to create stored procedures.
   - **Seed Scripts**: Located in the `scripts/seed` directory to populate the database with initial data.

4. **Edit `Init.sql` and `Hangfiredb.sql`**

   - **Init.sql**: Open the `Init.sql` file located in the `scripts` folder and update the API user credentials and permissions to match your security requirements.
   - **Hangfiredb.sql**: Open the `Hangfiredb.sql` file located in the `scripts` folder and change the default password to a strong, secure password.

5. **Run Init.sql**

   Execute the `Init.sql` script to set up the API user:

   ```bash
   mysql -u your_username -p gatekeeper_db < scripts/Init.sql
   ```

6. **Run Hangfiredb.sql**

   Execute the `Hangfiredb.sql` script to set up Hangfire:

   ```bash
   mysql -u your_username -p gatekeeper_db < scripts/Hangfiredb.sql
   ```

7. **Run Table Scripts**

   Execute all table creation scripts located in the `scripts/tables` directory:

   ```bash
   mysql -u your_username -p gatekeeper_db < scripts/tables/create_tables.sql
   ```

   *(If there are multiple table scripts, you can run them sequentially or create a script to execute all of them at once.)*

8. **Execute Stored Procedures**

   Run the stored procedures scripts located in the `scripts/stored_procedures` directory:

   ```bash
   mysql -u your_username -p gatekeeper_db < scripts/stored_procedures/create_procedures.sql
   ```

9. **Seed Initial Data**

   Populate the database with initial data by running the seed scripts:

   ```bash
   mysql -u your_username -p gatekeeper_db < scripts/seed/seed_data.sql
   ```

---

## Configuration

Proper configuration is crucial for GateKeeper to function correctly. This section covers the necessary configuration steps.

#### Automated Secret Setup with `setup-user-secrets.ps1`

    GateKeeper provides a PowerShell script, `setup-user-secrets.ps1`, located in the root of the repository, to simplify and automate the initialization and setting of user secrets for the `GateKeeper.Server` project. This script also handles the generation of the `Encryption:MasterKey` (referred to as `KeyManagement:MasterKey` in `secrets.template.json`).

    **Steps:**

    1.  **Prepare `secrets.template.json`:**
        *   In the root of the repository, you will find a `secrets.template.json` file. If it's missing, you can create one by copying the structure from `GateKeeper.Server/UserSecret.Example`.
        *   Open `secrets.template.json` and fill in all your required secret values (e.g., email server details, database connection strings, JWT key).
        *   Leave the `KeyManagement:MasterKey` field empty or as `""`; the script will generate and populate this value. The structure is as follows:

            ```json
            {
              "EmailSettings": {
                "SmtpServer": "your_smtp_server",
                "Port": "587",
                "UserName": "your_smtp_username",
                "Password": "your_smtp_password",
                "FromAddress": "noreply@example.com"
              },
              "Jwt": {
                "Key": "generate_a_strong_unique_key_here",
                "Issuer": "your_app_domain",
                "Audience": "your_app_domain",
                "TokenValidityInMinutes": 15,
                "RefreshTokenValidityInDays": 30
              },
              "DatabaseConfig": {
                "ConnectionString": "your_database_connection_string"
              },
              "ConnectionStrings": {
                "HangfireConnection": "your_hangfire_connection_string"
              },
              "KeyManagement": {
                "MasterKey": "" // Auto-generated by the script
              },
              "Resources": {
                "Path": "./"
              }
            }
            ```

    2.  **Run the Script:**
        *   Open a PowerShell terminal.
        *   Navigate to the root directory of the GateKeeper project.
        *   Execute the script:
            ```powershell
            .\setup-user-secrets.ps1
            ```
        *   The script will:
            *   Verify if `dotnet` CLI is available.
            *   Read values from your `secrets.template.json`.
            *   Generate a secure `MasterKey` for encryption.
            *   Navigate to the `GateKeeper.Server` directory.
            *   Run `dotnet user-secrets init` (if not already initialized).
            *   Set each secret using `dotnet user-secrets set "Section:Key" "Value"`.

    3.  **Verify User Secrets Configuration:**
        *   After the script runs, you can verify the secrets by right-clicking the `GateKeeper.Server` project in Visual Studio and selecting "Manage User Secrets", or by checking the `secrets.json` file located in `%APPDATA%\Microsoft\UserSecrets\<USER_SECRETS_ID>\secrets.json` (Windows) or `~/.microsoft/usersecrets/<USER_SECRETS_ID>/secrets.json` (Linux/macOS).
        *   Ensure your application is set up to read from user secrets. Typically, in `Program.cs` or `Startup.cs`, the configuration is already set to include user secrets in the development environment:
            ```csharp
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
            }
            ```
            This setup allows the application to securely access the configuration settings during development.

### Environment Variables

Sensitive information and environment-specific configurations can also be managed using environment variables to enhance security and flexibility.

1. **Set Environment Variables**

   Depending on your operating system, set the necessary environment variables. For example, on Windows:

   ```powershell
   $env:JWT_SECRET="YourSuperSecretKey"
   $env:DB_PASSWORD="YourDbPassword"
   ```

2. **Modify Configuration to Use Environment Variables**

   While GateKeeper primarily uses user secrets for managing sensitive configurations, you can also configure it to read from environment variables if needed. This step is optional and depends on your deployment strategy.

3. **Use a Configuration Provider**

   Ensure that your application is set up to read environment variables. In ASP.NET Core, this is typically handled automatically, but verify your `Program.cs` or `Startup.cs` to confirm.

---

## Running the Application

After completing the installation and configuration steps, you can run the GateKeeper application locally.

### Starting the Backend Server

1. **Navigate to the Backend Directory**

   ```bash
   cd GateKeeper.Server
   ```

2. **Run the Backend Application**

   Use the .NET CLI to run the backend server:

   ```bash
   dotnet run
   ```

   The server should start and listen on the configured port (e.g., `https://localhost:5001`).

### Starting the Frontend Application

1. **Open a New Terminal Window**

   Open a new terminal or command prompt window to run the frontend separately.

2. **Navigate to the Frontend Directory**

   ```bash
   cd gatekeeper.client
   ```

3. **Start the Angular Application**

   Use npm to start the frontend development server:

   ```bash
   npm start
   ```

   The frontend should be accessible at `http://localhost:4200` by default.

### Accessing the Application

1. **Open Your Browser**

   Navigate to `http://localhost:4200` to access the GateKeeper frontend.

2. **API Endpoints**

   The backend API is accessible at `https://localhost:5001/api/`. You can use tools like Postman or Swagger to explore the API endpoints.

---

## Testing

Ensuring that GateKeeper functions correctly through various tests is essential for maintaining quality and reliability.

### Running Unit Tests

GateKeeper includes unit tests for both the frontend and backend components.

#### Frontend Unit Tests

1. **Navigate to the Frontend Directory**

   ```bash
   cd gatekeeper.client
   ```

2. **Run Unit Tests**

   Execute the following command to run frontend unit tests using Karma and Jest:

   ```bash
   npm test
   ```

   **Notes:**
   - Ensure that all tests pass before making changes to the codebase.
   - Review test coverage reports to identify untested areas.

#### Backend Unit Tests

1. **Navigate to the Backend Directory**

   ```bash
   cd GateKeeper.Server
   ```

2. **Run Backend Tests**

   Use the .NET CLI to execute backend unit tests:

   ```bash
   dotnet test
   ```

   **Notes:**
   - Ensure that the test projects are correctly referenced.
   - Address any failing tests before proceeding with development.

### Running Integration Tests

Integration tests verify that different parts of the application work together as expected.

1. **Navigate to the Test Project Directory**

   ```bash
   cd GateKeeper.Server.Tests
   ```

2. **Run Integration Tests**

   Execute the following command:

   ```bash
   dotnet test --filter Category=Integration
   ```

   **Notes:**
   - Ensure that the test database is set up and configured correctly.
   - Use mock services or test environments to avoid affecting the production database.

---

## Deployment

Deploying GateKeeper to a production environment involves several considerations to ensure scalability, security, and reliability.

### Production Considerations

1. **Environment Configuration**

   - Use environment-specific configuration files (e.g., `appsettings.Production.json`) to manage settings for the production environment.
   - Securely manage secrets using environment variables or secret management tools like Azure Key Vault or AWS Secrets Manager.

2. **Database Management**

   - Ensure that the production database is secured and backed up regularly.
   - Implement database migration strategies to handle schema changes without downtime.

3. **Security Enhancements**

   - Enforce HTTPS across all endpoints.
   - Implement firewall rules and network security groups to restrict access to necessary ports and services.
   - Regularly update dependencies to patch security vulnerabilities.

4. **Scalability and Performance**

   - Utilize load balancers to distribute traffic across multiple instances.
   - Implement caching strategies using Redis or similar technologies to enhance performance.
   - Monitor application performance using APM tools like New Relic or Application Insights.

5. **Hangfire Integration**

   - Ensure that the Hangfire database (`hangfiredb`) is properly secured and that the password is strong and kept confidential.
   - Regularly monitor Hangfire jobs and queues to ensure they are running smoothly.

### Continuous Integration/Continuous Deployment (CI/CD)

Automate the build, testing, and deployment processes to ensure consistent and reliable releases.

1. **Choose a CI/CD Platform**

   Options include:
   - **GitHub Actions**
   - **Azure DevOps**
   - **Jenkins**
   - **GitLab CI/CD**

2. **Set Up CI/CD Pipelines**

   - **Build Pipeline**: Automate the build process for both frontend and backend components.
   - **Test Pipeline**: Integrate automated testing to run unit and integration tests on each commit.
   - **Deployment Pipeline**: Deploy the application to staging and production environments upon successful builds and tests.

3. **Implement Deployment Strategies**

   - **Blue-Green Deployment**: Reduce downtime by maintaining two identical production environments.
   - **Canary Releases**: Gradually roll out changes to a subset of users before a full release.
   - **Rollback Mechanisms**: Ensure the ability to revert to a previous stable version in case of issues.

---

## Contributing

We welcome contributions to enhance GateKeeper! Follow the guidelines below to contribute effectively.

### How to Contribute

1. **Fork the Repository**

   Click the **Fork** button on the repository page to create your own copy.

2. **Clone Your Fork**

   ```bash
   git clone https://github.com/yourusername/gatekeeper.git
   ```

3. **Create a Feature Branch**

   ```bash
   git checkout -b feature/your-feature-name
   ```

4. **Make Your Changes**

   Implement your feature or bug fix, ensuring adherence to coding standards and best practices.

5. **Run Tests**

   Ensure that all tests pass and that your changes do not introduce new issues.

6. **Commit Your Changes**

   ```bash
   git commit -m "Add feature: your feature description"
   ```

7. **Push to Your Fork**

   ```bash
   git push origin feature/your-feature-name
   ```

8. **Create a Pull Request**

   Navigate to the original repository and create a pull request from your fork.

### Guidelines

- **Code Standards**: Follow the project's coding conventions and best practices.
- **Commit Messages**: Write clear and descriptive commit messages.
- **Documentation**: Update documentation to reflect your changes where necessary.
- **Testing**: Include unit and integration tests for new features or bug fixes.
- **Issue Tracking**: Reference relevant issues in your pull requests to provide context.

For detailed contribution guidelines, please refer to the [CONTRIBUTING.md](CONTRIBUTING.md) file.

---

## License

GateKeeper is licensed under the [MIT License](LICENSE). You are free to use, modify, and distribute this software in accordance with the terms of the license.

---

## Contact

If you have any questions, issues, or suggestions regarding GateKeeper, feel free to reach out through the following channels:

- **Email**: [support@gatekeeper.com](mailto:support@gatekeeper.com)
- **GitHub Issues**: [GateKeeper Issues](https://github.com/yourusername/gatekeeper/issues)
- **Discussion Forum**: [GateKeeper Community Forum](https://forum.gatekeeper.com)

We appreciate your interest and contributions to the GateKeeper project!

---

# Additional Enhancements and Best Practices

To ensure the **Getting Started** guide is as helpful as possible, consider implementing the following best practices:

1. **Detailed Step-by-Step Instructions**: Provide clear and sequential steps for each process to minimize confusion.
2. **Screenshots and Visual Aids**: Include images or diagrams where applicable to guide users through the setup process.
3. **Troubleshooting Section**: Add a section addressing common issues and their solutions to assist users in resolving problems independently.
4. **FAQs**: Incorporate a Frequently Asked Questions section to address common queries and concerns.
5. **Code Examples**: Provide sample code snippets to illustrate configuration or usage scenarios.
6. **Glossary**: Define technical terms and acronyms used throughout the document to aid understanding.

Implementing these enhancements can further improve the usability and comprehensiveness of your documentation.

---

Feel free to customize and expand upon this guide to better fit the specific needs and workflows of your GateKeeper project. Let me know if you need further assistance or additional sections!