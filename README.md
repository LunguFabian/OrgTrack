# OrgTrack

## Overview
OrgTrack is an enterprise-grade web platform designed for the comprehensive management of national organizations with deep hierarchical structures (National -> Regions -> Committees -> Departments -> Teams). 

The primary goal of OrgTrack is to centralize organizational activities, meetings, events, task tracking, and reporting while providing real-time visibility into the performance and progress of every local unit.

## System Architecture

### Frontend
* **Framework:** Vue.js 3 (Composition API) with TypeScript
* **Styling:** Tailwind CSS for a modern, responsive, and robust UI
* **Features:** Component-based architecture, interactive dashboards, drag-and-drop Kanban boards, and integrated calendar views.
* **State Management:** Pinia
* **Routing:** Vue Router

### Backend
* **Framework:** ASP.NET Core 9 Web API
* **Architecture:** Clean Architecture (API -> Application -> Domain -> Infrastructure)
* **Authentication:** Google OAuth 2.0 coupled with JWT (JSON Web Tokens) and secure Refresh Tokens.
* **Security:** Recursive Hierarchical Role-Based Access Control (RBAC). Leaders automatically inherit management permissions for all descendant units.

### Database
* **System:** PostgreSQL
* **ORM:** Entity Framework Core
* **Core Entities:** Users, Roles, OrganizationUnits, Tasks, Events, EventRsvps, ActivityLogs.

## Core Features

### 1. User Management & Hierarchical Permissions
* **Tiered Roles:** National President, Regional Coordinator, Local Committee President, Vice President, Team Leader, and Member.
* **Granular Access Control:** Deep RBAC system with recursive inheritance. A Vice President has full access to their department and all underlying teams, but cannot access lateral departments unless explicitly invited.

### 2. Organizational Structure Tracking
* Dynamic tree-view visualization of the entire organization.
* Full CRUD operations for Regions, Committees, Departments, and Teams.
* Secure and validated member transfers between units.

### 3. Task Management & Kanban System
* Integrated Kanban boards (To Do, In Progress, Done) for every organizational unit.
* Task assignments, priority flags, deadlines, and tracking.
* Cross-unit visibility for higher-level management.

### 4. Events & Meeting Management
* Event scheduling and recurring meeting tracking.
* RSVP system and attendance tracking.
* (Planned) Google Calendar integration.

### 5. Analytics & Reporting Dashboard
* Real-time activity scoring based on task completion and event attendance.
* Dynamic bar charts and performance leaderboards for every department.
* Automated Activity Logs tracking system changes.

## Development Setup

### Prerequisites
* .NET 9 SDK
* Node.js (v18+)
* PostgreSQL server

### Backend Setup
1. Navigate to the API directory: `cd OrgTrack.Api`
2. Update the `appsettings.json` or user secrets with your PostgreSQL connection string and Google OAuth Client ID.
3. Apply database migrations: `dotnet ef database update`
4. Run the application: `dotnet run` (The database will automatically seed with dummy data in the Development environment).

### Frontend Setup
1. Navigate to the client directory: `cd orgtrack-client`
2. Install dependencies: `npm install`
3. Configure environment variables in `.env` (API URL, Google Client ID).
4. Run the development server: `npm run dev`

---
*Bachelor's Degree Project - National Organization Management & Tracking App*