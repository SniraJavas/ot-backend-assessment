

# OT Backend Assessment
## Online Betting Data Capture and Analytics System

## 1. Overview

This project implements an event-driven casino wager data capture and analytics system using .NET 10, RabbitMQ and Microsoft SQL Server.

The solution consists of three projects:

| Project | Responsibility |
|---|---|
| `OT.Assessment.App` | ASP.NET Core API for submitting wagers and retrieving wager information |
| `OT.Assessment.Consumer` | Background Worker Service that consumes wager messages and persists them |
| `OT.Assessment.Contracts` | Shared message contracts between the API and Consumer |

### Architecture

```text
Client / Tester
      |
      v
ASP.NET Core Web API
      |
      | POST casino wager
      v
RabbitMQ
      |
      | Consume message
      v
.NET Background Consumer
      |
      | Dapper / Stored Procedures
      v
SQL Server
      |
      +----------------------+
      |                      |
      v                      v
Player Wager History    Top Spenders
      ^                      ^
      |                      |
      +------ REST API ------+
```

### Main features

- Asynchronous casino wager ingestion.
- RabbitMQ-based message processing.
- SQL Server persistence using Dapper.
- Idempotent wager insertion.
- Paginated player wager history.
- Top-spenders leaderboard.
- SQL stored procedures and indexes.

## 2. Technology Stack

- .NET 10
- C#
- ASP.NET Core Web API
- .NET Worker Service
- RabbitMQ
- Microsoft SQL Server
- Dapper
- Docker Desktop
- Swagger
- NBomber and Bogus for performance testing.

## 3. Prerequisites

Before running the application, ensure the following are installed:

| Dependency | Purpose |
|---|---|
| .NET 10 SDK | Build and run the applications |
| SQL Server | Database |
| SQL Server Management Studio | Database management |
| Docker Desktop | Run RabbitMQ |
| Visual Studio 2022 or compatible IDE | Development |
| Git | Clone the repository |

Verify the .NET SDK installation:

```powershell
dotnet --version
```

## 4. Clone the Repository

```powershell
git clone <YOUR_PUBLIC_REPOSITORY_URL>
cd OT.Assessment
```

Restore the dependencies:

```powershell
dotnet restore
```

Build the solution:

```powershell
dotnet build
```

## 5. Database Setup

The database is created using the root-level SQL script:

```text
DatabaseGenerate.sql
```

### Instructions

1. Open SQL Server Management Studio.
2. Connect to your SQL Server instance.
3. Open `DatabaseGenerate.sql`.
4. Execute the script.
5. Verify that the database has been created.

Database name:

```text
OT_Assessment_DB
```

The script creates the required database objects, including:

- Players
- Providers
- Games
- CasinoWagers
- Primary and foreign keys
- Constraints
- Indexes
- Stored procedures

### Configure the database connection

Update the `ConnectionStrings` section in both:

- `OT.Assessment.App/appsettings.json`
- `OT.Assessment.Consumer/appsettings.json`

Example using Windows Authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=OT_Assessment_DB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Replace `YOUR_SQL_SERVER` with your SQL Server instance name.

For example:

```text
DESKTOP-JPLU5I5
```

Ensure the account running the API and Consumer has permission to connect to the database and execute the stored procedures.

## 6. RabbitMQ Setup

RabbitMQ runs in Docker using the management image.

### Pull the image

```powershell
docker pull rabbitmq:3-management
```

### Start the container

```powershell
docker run -d `
  --name ot-rabbitmq `
  -p 5672:5672 `
  -p 15672:15672 `
  -e RABBITMQ_DEFAULT_USER=otuser `
  -e RABBITMQ_DEFAULT_PASS=otpassword123 `
  rabbitmq:3-management
```

### Verify the container

```powershell
docker ps
```

Check the RabbitMQ logs:

```powershell
docker logs ot-rabbitmq
```

Wait until RabbitMQ has completed its startup.

### RabbitMQ Management Dashboard

Open:

http://localhost:15672

Example development credentials:

```text
Username: otuser
Password: otpassword123
```

### Configure RabbitMQ

Update the `RabbitMq` section in both API and Consumer `appsettings.json` files.

```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "otuser",
    "Password": "otpassword123",
    "VirtualHost": "/",
    "QueueName": "ot.assessment.queue"
  }
}
```

The API and Consumer must use the same broker configuration and queue name.

## 7. Run the Application

The application consists of two independently running processes.

Ensure SQL Server and RabbitMQ are running before starting them.

### Step 1: Start the Consumer

Open a terminal in:

```text
OT.Assessment.Consumer
```

Execute:

```powershell
dotnet run
```

The Consumer should connect to RabbitMQ and begin listening for messages on:

```text
ot.assessment.queue
```

Keep this terminal running.

### Step 2: Start the API

Open another terminal in:

```text
OT.Assessment.App
```

Execute:

```powershell
dotnet run --urls http://localhost:5000
```

The API should now be available at:

```text
http://localhost:5000
```

### Step 3: Open Swagger

Navigate to:

http://localhost:5000/swagger

You can now submit wagers and test the retrieval endpoints.

## 8. API Endpoints

### Submit Casino Wager

```http
POST /api/player/casinowager
```

Publishes a wager event to RabbitMQ.

Expected response:

```text
202 Accepted
```

### Retrieve Player Wager History

```http
GET /api/player/{playerId}/casinowagers?page=1&pageSize=10
```

Returns paginated wager history for a player.

### Retrieve Top Spenders

```http
GET /api/player/topspenders?count=10
```

Returns the highest-spending players in descending order of total wager amount.

## 9. Verify Wager Processing

After submitting a wager:

1. Confirm the API returns `202 Accepted`.
2. Check the Consumer terminal for message-processing logs.
3. Confirm the wager exists in SQL Server.

Example SQL:

```sql
SELECT *
FROM dbo.CasinoWagers
ORDER BY ReceivedAtUtc DESC;
```

### Verify duplicate handling

Submit the same wager again using the same `WagerId`.

The database should not create a duplicate record.

### Verify player wager history

Use the player wager history endpoint and confirm the returned records and pagination metadata.

### Verify top spenders

Use the top-spenders endpoint and confirm the players are returned in descending order of total wager amount.

## 10. Performance Testing

The supplied `OT.Assessment.Tester` project is used to benchmark the ingestion flow.

Configure its API endpoint:

```json
{
  "Api": {
    "EndpointUri": "http://localhost:5000/api/player/casinowager"
  }
}
```

Ensure the following are running:

- SQL Server
- RabbitMQ
- OT.Assessment.Consumer
- OT.Assessment.App

Run the supplied tester using its project instructions.

After the test finishes, allow the Consumer to process the remaining messages before checking the database.

Example reconciliation query:

```sql
SELECT
    COUNT(*) AS TotalWagers,
    COUNT(DISTINCT WagerId) AS UniqueWagers
FROM dbo.CasinoWagers;
```

Compare the final database records against the unique wager identifiers submitted by the tester.

Record the benchmark results, including successful submissions, failed submissions, processing time and final database reconciliation.

## 11. Configuration and Security Notes

The connection strings and RabbitMQ credentials shown in this README are development examples.

For production deployments:

- Use environment variables or a secrets manager.
- Avoid committing credentials to source control.
- Configure secure communication with RabbitMQ.
- Apply appropriate database permissions.
- Configure API authentication and authorisation.

## 12. Troubleshooting

| Problem | What to check |
|---|---|
| API cannot connect to RabbitMQ | Verify the container is running, port 5672 is exposed and credentials match |
| Consumer cannot connect to RabbitMQ | Verify broker configuration and RabbitMQ logs |
| SQL connection failure | Verify SQL Server instance name, authentication and database permissions |
| Stored procedure not found | Confirm `DatabaseGenerate.sql` executed successfully |
| API returns 503 | Check API logs and RabbitMQ availability |
| Wager not immediately visible | Check whether the Consumer has processed the queue message |
| Duplicate wager not inserted | Verify the same `WagerId` was submitted |



