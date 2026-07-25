BotPulse – Project Specification
Project Overview
BotPulse is an enterprise web application designed to provide centralized monitoring, operational
management, and analytics for Robotic Process Automation (RPA) environments.
The first supported platform will be UiPath Orchestrator, leveraging its official REST API to retrieve
operational information and execute management actions.
The application aims to provide a modern operational dashboard that simplifies robot administration,
execution monitoring, error analysis, and operational reporting.
Although the initial implementation focuses exclusively on UiPath, the overall architecture must remain
vendor-agnostic to facilitate future support for additional RPA platforms such as Microsoft Power Automate,
Automation Anywhere, and Blue Prism.
Objectives
The primary objectives of BotPulse are:
• 
• 
• 
• 
• 
• 
• 
• 
Centralize RPA monitoring.
Improve operational visibility.
Reduce incident response time.
Provide a modern management interface.
Simplify robot administration.
Offer deployment flexibility.
Minimize infrastructure dependencies.
Maintain a clean and extensible architecture.
Initial Scope (MVP)
The first version of BotPulse will support:
• 
• 
• 
• 
• 
• 
Robot monitoring.
Job monitoring.
Queue monitoring.
Process monitoring.
Machine monitoring.
Asset visualization.
1
• 
• 
• 
• 
• 
• 
• 
• 
Execution history.
Error visualization.
Execution logs.
Dashboard with operational KPIs.
Start Processes.
Stop/Cancel Running Jobs.
Retry Failed Jobs (when supported).
Health monitoring.
Non-Goals
The first version will NOT include:
• 
• 
• 
• 
• 
• 
Multi-RPA support.
User provisioning.
AI features.
Predictive analytics.
Notification engine.
Multi-tenant SaaS architecture.
These features should remain possible through future architectural extensions.
Architecture Principles
The project must follow Clean Architecture principles.
Business logic must remain completely independent from:
• 
• 
• 
• 
• 
UiPath
ASP.NET Core
Databases
Docker
Infrastructure
The application should be easily testable and highly maintainable.
Dependency Injection should be used throughout the solution.
SOLID principles must be respected.
2
Solution Structure
The solution should be organized into multiple projects.
BotPulse.Api
Responsible for exposing REST APIs.
BotPulse.Core
Contains business logic, interfaces, domain models, and application services.
BotPulse.Infrastructure
Contains persistence, repositories, configuration, logging, and external services.
BotPulse.UiPath
Contains all communication with UiPath Orchestrator.
No UiPath-specific code should exist outside this project.
BotPulse.Worker
Background synchronization service responsible for periodically retrieving information from UiPath.
BotPulse.Shared
Shared DTOs, constants, models, and utilities.
UiPath Integration
Communication with UiPath must occur exclusively through the official REST API.
Authentication should use OAuth2 Client Credentials.
The implementation must isolate all UiPath-specific communication behind provider interfaces.
Example:
IRpaProvider
3
Methods may include:
• 
• 
• 
• 
• 
• 
• 
GetRobots()
GetJobs()
GetQueues()
GetProcesses()
StartJob()
StopJob()
GetLogs()
Future providers should be interchangeable without modifying the application core.
Deployment Strategy
The application must support multiple deployment models.
The first supported deployment model is:
On-Premise
The customer installs BotPulse inside their own infrastructure.
The application communicates securely with UiPath Automation Cloud or UiPath On-Prem.
Future deployment models may include:
• 
• 
• 
• 
SaaS
Azure App Service
Docker Cloud
Kubernetes
The architecture should not depend on any specific hosting environment.
Docker Strategy
Docker is the primary packaging mechanism.
The application should be fully containerized.
Benefits include:
• 
• 
Simplified deployment
Environment consistency
4
• 
• 
• 
Easy upgrades
Platform independence
Reduced installation complexity
A Docker Compose configuration should orchestrate all required services.
The deployment should be executable with a single command.
Future environments should require minimal configuration changes.
Configuration
Application configuration must be externalized.
Sensitive information must never be hardcoded.
Configuration values include:
• 
• 
• 
• 
• 
• 
• 
UiPath URL
Tenant
Organization
Client ID
Client Secret
Refresh Interval
Logging Level
Configuration sources should support:
• 
• 
• 
appsettings.json
Environment Variables
Secret Managers
Security
Credentials must be encrypted whenever stored.
HTTPS must be enforced.
JWT authentication should be supported.
Role-based authorization should be implemented.
Sensitive configuration must remain outside source control.
5
Logging
Structured logging should be implemented using Serilog.
Logs should support multiple sinks.
The system should generate operational logs and audit logs separately.
Performance
The dashboard should avoid excessive calls to UiPath.
Operational data should be synchronized through background workers.
The UI should retrieve cached information whenever possible.
The architecture should support thousands of executions without significant performance degradation.
Future Extensibility
The architecture should support future modules including:
• 
• 
• 
• 
• 
• 
• 
• 
• 
• 
Power Automate Provider
Automation Anywhere Provider
Blue Prism Provider
Notification Engine
SLA Monitoring
AI Insights
Power BI Connector
Teams Integration
Email Alerts
Mobile Dashboard
No architectural redesign should be required to support these future modules.
6
Technical Stack
Backend
• 
• 
• 
ASP.NET Core 8
C#
REST API
Frontend
• 
React + TypeScript (preferred)
Persistence
• 
• 
PostgreSQL (preferred)
SQL Server (supported)
Containerization
• 
• 
Docker
Docker Compose
Authentication
• 
• 
OAuth2
JWT
Logging
• 
Serilog
Documentation
• 
Swagger / OpenAPI
Version Control
• 
• 
Git
GitHub
Design Philosophy
BotPulse should feel like a modern enterprise operations center rather than a traditional administration
portal.
7
The user experience should prioritize operational efficiency, fast navigation, real-time visibility, and intuitive
management of RPA environments.
Every architectural decision should favor maintainability, scalability, and future extensibility over short-term
implementation convenience.
8