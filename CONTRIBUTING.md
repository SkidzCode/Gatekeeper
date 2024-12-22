# Contributing to GateKeeper

Thank you for considering contributing to GateKeeper! We appreciate your interest in improving the project and value your time and effort.

## How to Contribute

### Reporting Bugs

If you encounter a bug, please:
1. Search the [issues](https://github.com/SkidzCode/GateKeeper/issues) to ensure it hasn’t been reported already.
2. If no existing issue matches, create a new issue with:
   - A clear and descriptive title.
   - Steps to reproduce the issue.
   - Expected and actual results.
   - Environment details (e.g., OS, .NET version, database version).

### Suggesting Features

To suggest a feature, please:
1. Check the [issues](https://github.com/SkidzCode/GateKeeper/issues) and [discussions](https://github.com/SkidzCode/GateKeeper/discussions) for similar ideas.
2. Create a new issue or discussion post with:
   - A descriptive title.
   - A clear description of the feature and its benefits.
   - Any relevant context or examples.

### Making Changes

#### Setting Up the Project Locally

1. Fork the repository and clone it to your local machine.
2. Ensure you have the following prerequisites:
   - .NET 8 SDK
   - Node.js and npm
   - MariaDB 10
3. Set up the database:
   - Run the table scripts, stored procedures, and other scripts in the `script` folder.
4. Configure the `appsettings.json` file with your environment details (see the README for structure).

#### Coding Guidelines

Please adhere to the following coding standards:
- Use consistent naming conventions.
- Follow the SOLID principles.
- Write clear and concise comments where necessary.
- Ensure your code is covered by unit tests.

#### Submitting a Pull Request

1. Create a branch for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```
2. Make your changes, committing frequently with meaningful commit messages.
3. Test your changes thoroughly.
4. Push your branch to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```
5. Create a pull request (PR) to the `main` branch of the GateKeeper repository. Include:
   - A clear title and description of your changes.
   - Reference to any related issues (e.g., "Fixes #123").

### Code Reviews

All PRs will be reviewed by a maintainer. Please be responsive to feedback and make necessary changes promptly.

### Testing

Before submitting your changes, ensure:
- All unit tests pass.
- New tests are added for new features or bug fixes.
- No existing functionality is broken.

### Documentation

If your changes affect the project’s functionality, update the documentation accordingly (e.g., README, in-code comments).

## Community Guidelines

- Be respectful and constructive in all communications.
- Provide context and details when raising issues or proposing changes.
- Follow the project’s code of conduct.

## Questions or Assistance

If you have any questions, feel free to reach out by creating a discussion or contacting us directly as mentioned in the [README](README.md).

We look forward to your contributions!

---

Thank you for helping us improve GateKeeper!

