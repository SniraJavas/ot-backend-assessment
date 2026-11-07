# Online Betting Data Capture and Analytics System

## Scenario
In the dynamic world of online betting, capturing and analyzing data is paramount for business success. Every time a player initiates a spin in a casino game, it's essential to capture and store relevant data. This project aims to develop an API and service capable of receiving, storing, and retrieving player casino data.

## Purpose of the Assessment
Your solution should showcase your experience, standards, and passion for software development. Submit work that reflects the quality we can expect from you if you join our team.

The tester is provided. You must implement the API, consumer service, and database schema, including the database creation script and setup instructions.

Good luck and have fun.

## Pre-Requisites
- **Project Boilerplate:** Download and extract [ot-backend-assessment.zip](../ot-backend-assessment.zip). Open the extracted `ot-backend-assessment` folder containing `src`, `test`, and this README. Run the commands below from that folder.

- **Message Bus:** RabbitMQ, Kafka, Redis, etc.

    Document your message bus setup in the submission notes. For example, describe how to run a local RabbitMQ instance on `localhost` with credentials `guest/guest`.

    > [Run RabbitMQ Windows Service](https://www.rabbitmq.com/docs/install-windows#installer) or
 
    > [Docker Compose Rabbit Image](docker/RabbitMQ/docker-up.bat)
- **SQL Server:** Setup a local [SQL Server Developer Edition](https://www.microsoft.com/en-za/sql-server/sql-server-downloads) or docker compose with the connection string: `"SERVER=localhost; DATABASE=OT_Assessment_DB; Integrated Security=SSPI;"`.


## Requirements Tasks
### 1. .NET API
The API (OT.Assessment.App) must at least include the following endpoints:
- **POST** `api/player/casinowager`: Receives player casino wager events and publishes them to your chosen message bus (RabbitMQ, Kafka, Redis, etc.).
    ```json
    {
      "wagerId": "aa6700eb-1a06-483e-9739-d293dc7a9383",
      "theme": "adventure",
      "provider": "Ergonomic Soft Fish",
      "gameName": "Ergonomic Granite Cheese",
      "transactionId": "410b7161-3473-4d74-85c3-a533d050a9d3",
      "brandId": "8a2016f8-c4c4-471f-9a9c-337a54664650",
      "accountId": "5ac75fec-23e9-27d1-b660-179eee70003d",
      "Username": "Jay.Bernhard67",
      "externalReferenceId": "0267dbca-2760-4a9e-ab42-5ce766fa8ca0",
      "transactionTypeId": "8aaece0c-5d53-4225-a937-adb454c4da31",
      "amount": 38273.974454660885,
      "createdDateTime": "2024-05-04T02:25:05.9906387+02:00",
      "numberOfBets": 3,
      "countryCode": "BS",
      "sessionData": "Central Chile Awesome Cotton Gloves cross-platform Handmade Rubber Shoes portals leading-edge Coordinator Data Producer end-to-end encoding Gorgeous Clothing View Health, Outdoors & Music embrace Metrics Facilitator morph",
      "duration": 1827254
    }
    ```
- **GET** `api/player/{playerId}/casino?pageSize=10&page=1`: Returns a paginated list of the latest casino wagers for a specific player. The `{playerId}` route parameter is the `accountId` supplied in the wager payload.
    ```json
    {
      "data": [
        {
          "wagerId": "aa6700eb-1a06-483e-9739-d293dc7a9383",
          "game": "Ergonomic Granite Cheese",
          "provider": "Ergonomic Soft Fish",
          "amount": 500,
          "createdDate": "2024-09-10T07:25:13.577Z"
        },
        {
          "wagerId": "aa6700eb-1a06-483e-9739-d293dc7a9383",
          "game": "Test",
          "provider": "Test",
          "amount": 250,
          "createdDate": "2024-09-10T07:25:13.577Z"
        },
        //etc
      ],
      "page": 1,
      "pageSize": 10,
      "total": 35,
      "totalPages": 4
    }
    ```
- **GET** `api/player/topSpenders?count=10`: Returns the top {count} players based on total spending. Highest to Lowest.
    ```json
    [
      {
        "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "username": "testUser1",
        "totalAmountSpend": 150
      },
      {
        "accountId": "aa6700eb-5717-4562-b3fc-2c963f66bev7",
        "username": "testUser21",
        "totalAmountSpend": 100
      },
      //etc
    ]
    ```

### 2. .NET Service
This service (OT.Assessment.Consumer) needs to consume messages published to the aforementioned queue and store consumed messages in the SQL database.

### 3. SQL Database
Design the tables, indexes, keys, stored procedures, etc. necessary to store and retrieve the data required above. Follow database best practices and minimise unnecessary duplication. Not all payload fields need to be stored; use your discretion to retain the data needed to meet the requirements.

**DatabaseGenerate.sql**: Populate the file in the root of the directory with the necessary statements to recreate the database.

### 4. Benchmark and Test
Test your application by running the <b>OT.Assessment.Tester</b>.

The tester requires the **.NET 10 SDK** and uses **Bogus** to generate casino wager data and **NBomber** to send it to your API.

1. First, run the API Application and the Consumer Service simultaneously.
2. Edit [test/OT.Assessment.Tester/appsettings.json](test/OT.Assessment.Tester/appsettings.json) and set `Api:EndpointUri` to the full URL of your running API's casino wager POST endpoint, including its scheme, port, and route. For the required assessment route, for example:
   ```json
   {
     "Api": {
       "EndpointUri": "http://localhost:5000/api/player/casinowager"
     }
   }
   ```
   The supplied setting is `http://localhost:5000/api`. Replace it with your actual wager endpoint before running the tester. The tester posts directly to `Api:EndpointUri` as configured; it does not append `/player/casinowager` or any other route. Use an absolute `http://` or `https://` URI without a fragment (`#...`); query strings are supported if needed. For local HTTPS, ensure your development certificate is trusted (`dotnet dev-certs https --trust`).
3. From the extracted assessment root directory (the folder containing `test`), run:
   ```powershell
   dotnet run --project .\test\OT.Assessment.Tester\OT.Assessment.Tester.csproj
   ```
   The settings file is copied to the build and publish output and read from alongside the tester executable. The tester validates `Api:EndpointUri` before starting the load test and prints the target endpoint.
4. The tester posts Bogus-generated casino wagers to your configured API, populating the queue with relevant data. Concurrently, your consumer application should process incoming messages from the queue and store them within your database.
5. Debug, optimise and ensure your application can handle the load by storing all the messages.
6. Ensure both GET endpoints return data during the load test and after the load test.

**Load-test success:** Every wager submitted by the tester must eventually be stored in the database. The queue may continue draining after the tester finishes; keep the consumer running until processing is complete. Check for failed submissions and processing errors, and reconcile the submitted wagers with the stored records to demonstrate that no wagers were lost. Both GET endpoints must remain usable during ingestion and return correct results once processing is complete. Record the test results, any failures, and the total time taken to store all wagers in your submission notes.

### 5. BONUS
Surprise us! Add some things to your project you think are valuable additions. This will help you stick out from the rest of the candidates so try be creative.

### 6. Notes
Document your design choices and reasoning in [Notes.md](Notes.md). Include setup instructions, challenges encountered, and suggested improvements for a production-ready solution.

## Scoring Criteria
- Completion of all requirement tasks as specified above.
- Architecture and project structure.
- Naming conventions for variables, projects, tables, and stored procedures.
- Adherence to SOLID principles.
- Readability and maintainability of the code.
- Efficient database design.
- Efficiently consuming all the data from the tester application as quickly as possible.
- Bonus features or additions that demonstrate passion, innovation, and quality.
- Proficiency and adherence to best practices for .NET projects, background services, message queues, RESTful APIs, Git, and SQL Server.

## FAQ
- Can I modify the suggested solutions, projects, and classes? <b> Yes, it is encouraged but ensure all pre-requisites and tasks are completed as stated above.</b>
- What technologies should I use? <b>Feel free to use any packages you're comfortable with in the .NET ecosystem.</b>
- How do I test my application? <b>Run the tester application to simulate sending casino game data to your API. Ensure both GET routes return the correct data</b>
- How is my application tested and assessed? <b>We'll generate the database from your schema script, start the API and consumer, run the tester application, and evaluate against the scoring criteria</b>
- Can I use AI? <b>We encourage you to complete the assessment yourself and limit AI assistance to routine boilerplate tasks. Your submission must demonstrate your own engineering judgement. You are responsible for the correctness and quality of all submitted code and must be able to explain your implementation and design decisions in a follow-up interview.</b>
- Need clarification? <b>Compile a list of questions and send them to your OT representative for prompt assistance</b>
- What's next after completing and testing the solution? <b>Ensure your database schema script and submission notes are complete. Commit your code to a public GitHub repository and share the link with your OT representative. Document any further enhancements needed for production readiness.</b>

Try to remember and briefly document the decisions you made and why you made them. We may ask you in a follow-up interview.

##### -------------Documentation of the Solution --------------

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

## 13. Project Documentation

For the architecture walkthrough, database design explanations, stored procedure breakdown, implementation decisions and engineering interview preparation, see:

```text
INTERVIEW_NOTES.md
```

