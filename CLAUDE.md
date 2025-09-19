# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

This is a .NET 8 AWS Lambda-based event-driven system that processes user creation events from SQS and sends welcome emails. The project follows a layered architecture:

### Layer Structure
- **Function Layer**: AWS Lambda entry point (`Fiap.Events.Functions`)
- **Application Layer**: Business logic and services (`Fiap.Events.Services`)
- **Infrastructure Layer**: External integrations and repositories (`Fiap.Events.Infra`, `Fiap.Contracts.Events`)
- **Tests Layer**: Unit tests (`Fiap.Events.Unit.Tests`)

### Key Components
- **Function.cs**: Main Lambda handler that processes SQS events containing user creation messages
- **EventService**: Application service that orchestrates email sending
- **EmailService**: Infrastructure service that handles email delivery
- **CreateUserIntegrationEvent**: Contract for user creation events from CRM system

### Event Flow
1. CRM system sends user creation message to SQS queue `lambda-queue`
2. Lambda function is triggered by SQS messages
3. Function deserializes the message (supports multiple formats: Rebus envelope, Base64 encoded, plain JSON)
4. Maps event to CreateAccount model and calls EventService
5. EventService uses EmailService to send welcome email

## Common Development Commands

### Build and Test
```bash
# Create temporary solution from .slnx (required for CI/CD)
dotnet new sln -n Fiap
find . -name "*.csproj" -exec dotnet sln Fiap.sln add {} \;

# Restore dependencies
dotnet restore *.sln

# Build solution
dotnet build *.sln --no-restore --configuration Release

# Run all tests
dotnet test *.sln --no-build --configuration Release --verbosity normal
```

### Local AWS Lambda Development
- Install AWS CLI and configure with IAM credentials (region: sa-east-1)
- Install AWS Toolkit for Visual Studio
- Use SQS example request for testing (see README.md for format)

## AWS Configuration

### Required Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `AWS_REGION`: sa-east-1 (default)
- `SSM_PATH`: /myapp/prod/ (default) - for AWS Systems Manager parameters

### Configuration Sources (in order of precedence)
1. Environment variables
2. AWS Systems Manager parameters
3. User secrets (Development only)
4. appsettings.{Environment}.json
5. appsettings.json

## Testing Strategy

The codebase uses xUnit for unit testing with tests organized by layer:
- Function Layer Tests: Lambda handler testing
- Application Layer Tests: Service logic testing (EventService, CRMService, FCGService)
- Infrastructure Layer Tests: External service integration testing (EmailService)

## CI/CD

### GitHub Actions Workflows
- **validate-pr.yml**: Validates PRs to main/dev branches with build and test
- **validate-release-pr.yml**: Pre-validation for release/hotfix PRs
- **release.yml**: Automated release on main branch
- **deploy-aws.yml**: AWS ECS deployment

### Deployment
- AWS Lambda deployment with SQS trigger
- ECS deployment for supporting services
- Uses GitHub Actions for automated deployment pipeline