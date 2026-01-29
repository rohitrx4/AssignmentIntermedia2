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
dotnet run --project .\OrderServiceNotifications\OrderServiceNotifications.csproj --no-launch-profile


Running Notification Service Locally

Use the following PowerShell command to run the Notification Service by manually setting the required environment variables.

Set-Location 'C:\Users\Dell\Desktop\AssignmentIntermedia'
$env:ConnectionStrings__NotificationConnection = 'Server=localhost,1433;User Id=sa;Password=Your_password123;Database=NotificationDb;TrustServerCertificate=True;'
$env:RABBITMQ_HOST = 'localhost'
$env:RABBITMQ_USER = 'user'
$env:RABBITMQ_PASS = 'password'
$env:ASPNETCORE_URLS = 'http://localhost:5002'
dotnet run --project .\NotificationService\NotificationService.csproj --no-launch-profile

