# StrikeData ⚾📊

Aplicación web en **ASP.NET Core Razor Pages** para consultar y gestionar estadísticas de béisbol, obtenidas de la **API oficial de la MLB** y de **TeamRankings**.  

Este proyecto centraliza las estadísticas en una base de datos PostgreSQL, permitiendo explorarlas a través de una interfaz web ligera y extensible.

---

## 🚀 Características principales
- Obtención de estadísticas acumuladas desde varias APIs.
- Scraping a varias aplicaciones webs.
- Cálculo de estadísticas derivadas desde páginas como **TeamRankings**.
- Persistencia con **Entity Framework Core**.
- Interfaz web con **Razor Pages**.
- Soporte inicial para **splits** (home/away, vs LHP/RHP, últimos partidos).
- Preparado para ejecutar importaciones periódicas de datos.

---

## 📂 Estructura del proyecto

La organización de carpetas sigue una arquitectura sencilla para separar responsabilidades:

- **`Data/`** → Configuración de Entity Framework Core y `AppDbContext`.  
- **`Models/`** → Entidades de dominio (ej. `Team`, `TeamStat`, `StatType`).  
- **`Services/`** → Lógica de negocio e importadores (MLB API, TeamRankings).  
- **`Pages/`** → Páginas Razor que conforman la interfaz web.  
- **`Migrations/`** → Migraciones generadas con EF Core para versionar la base de datos.  
- **`wwwroot/`** → Archivos estáticos (CSS, JS, imágenes).  
- **`Program.cs`** → Punto de entrada y configuración principal de la aplicación.  
- **`appsettings.json`** → Configuración de la aplicación (cadenas de conexión, opciones, etc.).

---

## 📦 Requisitos previos
Antes de ejecutar el proyecto, asegúrate de tener instalado:

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) (versión 13 o superior recomendada)
- [EF Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)  
  ```bash
  dotnet tool install --global dotnet-ef

---

## 🔧 Configuración del entorno

1. _Clonar el repositorio_

git clone https://github.com/braiss13/StrikeData.git


cd StrikeData

2. _Restaurar dependencias_

dotnet restore

3. _Configurar la base de datos PostgreSQL_

    3.1 -> Crear la base de datos: 
            CREATE DATABASE strikedata;

    3.2 -> Editar el archivo appsettings.Development.json y añadir la cadena de conexión: 
            {
                {
                    "ConnectionStrings": {
                        "DefaultConnection": "Host=localhost;Port=5432;Database=strikedata;Username=postgres;Password=YOURPASSWORD"
                    }
                }
            }
            
    ¡¡ _Reemplazar YOURPASSWORD por tu contraseña de POSTGRESQL_ !!

4. _Aplicar migraciones_

dotnet ef database update

5. _Ejecutar el proyecto_

dotnet run

---

## 🛠 Uso

1. Inicia la aplicación con dotnet run.

2. Abre el navegador en https://localhost:5001.

3. Explora estadísticas de equipos.

4. El servicio de StrikeData se encarga de obtener y actualizar los datos en la base de datos.

---

## 📄 Licencia
Este proyecto está bajo la licencia MIT.  
© 2025 [braiss13](https://github.com/braiss13)