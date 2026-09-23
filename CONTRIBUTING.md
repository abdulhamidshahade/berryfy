# Contributing to Berryfy

First off, thank you for considering contributing to Berryfy! 

## Local Development Setup

Berryfy is a full-stack e-commerce platform. To run the project locally, you will need the following installed:
* .NET 9 SDK
* Node.js
* Docker & Docker Compose
* PostgreSQL 

**Steps to run:**
1. Clone the repository: `git clone https://github.com/abdulhamidshahade/berryfy.git`
2. Start the database and infrastructure via Docker Compose: `docker-compose up -d`
3. Navigate to the backend directory and apply Entity Framework Core migrations: `dotnet ef database update`
4. Run the .NET API: `dotnet run`
5. Navigate to the frontend directory, install dependencies, and start the Next.js development server: `npm install && npm run dev`

## Architecture & Coding Standards

To maintain a scalable and manageable codebase, please adhere to the following patterns when submitting backend changes:
* **Modular Monolith & Clean Architecture:** Ensure core business logic remains strictly isolated from infrastructure concerns.

For the Next.js frontend, favor React Server Components where appropriate and ensure clean, responsive interface designs.

## Commit Guidelines

* Write clear, concise commit messages in the imperative mood (e.g., "Add payment gateway integration" rather than "Added payment gateway").
* Ensure your Git author name is properly configured to use your real, standardized name (e.g., `git config --global user.name "Hamit Şahade"`) rather than informal handles or inconsistent aliases.

## Submitting a Pull Request

1. Fork the repository and create your feature branch from `master`.
2. If you've added code that should be tested, add tests.
3. Ensure the application builds and runs successfully in your local Docker environment.
4. Open a Pull Request and fill out the provided PR template completely.
