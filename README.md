This repository contains four projects related to the assignment. Out of these four projects, the Order Service and Notification Service are the main runtime services. The remaining projects are supporting or shared components and do not need to be executed independently.

Both Order Service and Notification Service are containerized and can be deployed using Docker or Kubernetes. However, due to configuration related issues while running inside a container or pod, these two services are recommended to be run locally during development and evaluation.

When the project is deployed on a pod, some environment variables such as database connection strings and RabbitMQ credentials are not picked up automatically. In such cases, the services must be started manually by passing all required configuration values through the command line.




Running Order Service Locally

Use the following PowerShell command to run the Order Service by manually setting the required environment variables.

Set-Location 'C:\Users\Dell\Desktop\AssignmentIntermedia'
$env:ConnectionStrings__DefaultConnection = 'Server=localhost,1433;User Id=sa;Password=Your_password123;Database=OrderDb;TrustServerCertificate=True;'
$env:RABBITMQ_HOST = 'localhost'
$env:RABBITMQ_USER = 'user'
$env:RABBITMQ_PASS = 'password'
$env:ASPNETCORE_URLS = 'http://localhost:5000'
dotnet run --project .\OrderServiceNotifications\OrderService.csproj --no-launch-profile


Running Notification Service Locally

Use the following PowerShell command to run the Notification Service by manually setting the required environment variables.

Set-Location 'C:\Users\Dell\Desktop\AssignmentIntermedia'
$env:ConnectionStrings__NotificationConnection = 'Server=localhost,1433;User Id=sa;Password=Your_password123;Database=NotificationDb;TrustServerCertificate=True;'
$env:RABBITMQ_HOST = 'localhost'
$env:RABBITMQ_USER = 'user'
$env:RABBITMQ_PASS = 'password'
$env:ASPNETCORE_URLS = 'http://localhost:5002'
dotnet run --project .\NotificationService\NotificationService.csproj --no-launch-profile

*Prerequisites

The following software and tools must be installed before running this project.
.NET SDK version 8 or higher must be installed on the system. This project is built and tested using .NET 8.
Docker Desktop must be installed and running. Docker is required to run SQL Server and RabbitMQ using Docker Compose.
Docker Compose must be available. It is included with recent versions of Docker Desktop.
Microsoft SQL Server must be available either through Docker or locally. The project uses SQL Server as the relational database.
RabbitMQ must be available either through Docker Compose or as a local installation. This project uses RabbitMQ for asynchronous messaging.
PowerShell is required on Windows to run the provided environment variable commands.
Git must be installed to clone the repository if the source code is not downloaded as a ZIP file.

*Project Structure Overview
The repository contains four projects in total.
Order Service is the main service that exposes REST APIs and produces events.
Notification Service is the consumer service that processes events and stores notification history.
The remaining projects are shared or supporting components and are not required to be executed independently.

*Initial Setup Guidelines
Clone the repository or extract the ZIP file to a local directory.
Open the project root folder in a terminal or PowerShell window.
Ensure Docker Desktop is running before proceeding.
Verify that ports required by SQL Server, RabbitMQ, and the services are free on the local machine.
Running Infrastructure Using Docker Compose
Navigate to the directory containing the docker-compose.yml file.
Run the Docker Compose build command to start SQL Server and RabbitMQ.
Wait until all containers are fully started and healthy before running the services.
Confirm that SQL Server is accessible on port 1433.
Confirm that RabbitMQ is accessible on the configured host and port.

*Database Preparation Guidelines
The Order Service uses its own database named OrderDb.
The Notification Service uses its own database named NotificationDb.
Databases will be created automatically when the services start, if migrations are configured.
Ensure the SQL Server credentials match the values provided in the environment variables.
Running Order Service for the First Time

The Order Service must be started with required environment variables.
These variables include database connection string, RabbitMQ host details, and application URL.
If the service is deployed inside a container or pod and configuration is not resolved automatically, the service must be started manually using PowerShell commands provided in this document.
Once started, verify the service is running by accessing the Orders API endpoint.
Running Notification Service for the First Time
The Notification Service must be started after RabbitMQ is running.
This service also requires database and RabbitMQ configuration through environment variables.
Manual startup commands must be used if the service is running outside Docker or if configuration injection fails.
Once started, verify the service by accessing the Notifications API endpoint.




