# StrikeData ⚾📊

Web application in **ASP.NET Core Razor Pages** to query and manage baseball statistics, obtained from **MLB APIs**, **TeamRankings**, and **Baseball-Almanac**.  

This project centralizes statistics in a PostgreSQL database, allowing them to be explored through a lightweight and extensible web interface.

---

## 🚀 Main Features
- Retrieval of aggregated statistics from multiple APIs.
- Scraping of various web applications.
- Calculation of derived statistics from sites like **TeamRankings**.
- Quick explanation and visualization of statistics.
- Persistence with **Entity Framework Core**.
- Web interface with **Razor Pages**.
- Development using **ASP.NET (C#)**

---

## 📂 Project Structure

The folder organization follows a simple architecture to separate responsibilities:

- **`Data/`** → Entity Framework Core configuration and `AppDbContext`.  
- **`Models/`** → Domain entities (e.g. `Team`, `TeamStat`, `StatType`).  
- **`Services/`** → Business logic and importers (MLB API, TeamRankings).  
- **`Pages/`** → Razor Pages that make up the web interface.  
- **`Migrations/`** → EF Core migrations for database versioning.  
- **`wwwroot/`** → Static files (CSS, JS, images).  
- **`Program.cs`** → Entry point and main application configuration.  
- **`appsettings.json`** → Application configuration (connection strings, options, etc.).

---

## 📦 Prerequisites
Before running the project, make sure you have installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) (version 13 or higher recommended)
- [EF Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)  
  ```bash
  dotnet tool install --global dotnet-ef

---

## 🔧 Environment Setup

1. _Clone the repository_
    ```bash
    git clone https://github.com/braiss13/StrikeData.git

    cd StrikeData

2. _Restore dependencies_
    ```bash
    dotnet restore

3. _Configure the PostgreSQL database_

    3.1 -> Create the database: 

            CREATE DATABASE strikedata;

    3.2 -> Edit the appsettings.Development.json file and add the connection string: 

            {
                {
                    "ConnectionStrings": {
                        "DefaultConnection": "Host=localhost;Port=5432;Database=database_name;Username=database_user;Password=user_password"
                    }
                }
            }
            
    ⚠️ _Replace the database name, user, and password parameters_ 

4. _Apply migrations_
    ```bash
    dotnet ef database update

5. _Run the project_
    ```bash
    dotnet run

---

## 🛠 Usage

1. Start the application with dotnet run.

2. Open your browser at https://localhost:7081.

3. The StrikeData service is responsible for retrieving and updating the data in the database.

4. You can now access the different sections and get the desired statistics.
