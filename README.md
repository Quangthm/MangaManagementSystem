# MangaManagementSystem
> Distributed monolith platform for manga/webtoon production, AI-assisted segmentation, editorial review, and ranking management.

## 1. Project Overview

Manga Management System is a student software engineering project designed to support the full lifecycle of manga creation and publication.

The system is built as a **Distributed Monolith**, where the core business system remains unified while the AI-heavy processing is separated into a local microservice.

The project covers:

- identity and governance,
- studio production workflow,
- AI-assisted segmentation,
- editorial and publishing workflow,
- business analytics and ranking,
- audit and transparency.

---

## 2. Current Repository Status

**PRIVATE DEVELOPMENT REPOSITORY**

This repository is currently used for internal team collaboration only.

The codebase may contain:

- unfinished features,
- experimental implementations,
- incomplete documentation,
- temporary test data,
- unstable APIs.

Do not treat this repository as production-ready.

---

## 3. System Architecture

### 3.1 Overall Architecture

The system follows a **Distributed Monolith** architecture:

- **Frontend**: Blazor Server
- **Backend**: ASP.NET Core Web API (.NET 8/9)
- **AI Microservice**: Python FastAPI + YOLOv8
- **Database**: SQL Server with Entity Framework Core
- **Storage**: Cloudinary
- **Real-time communication**: SignalR

### 3.2 Frontend

The frontend uses:

- **Blazor Server** for interactive server-rendered UI
- **MudBlazor** for admin and management pages
- **Fabric.js** for canvas-based manga page interaction
- **Webtoon-style vertical scrolling** for reader/editor views

### 3.3 Backend

The backend uses:

- **ASP.NET Core Web API**
- **Clean Architecture**
  - Domain
  - Application
  - Infrastructure
  - API

This keeps business logic separated from database access and UI.

### 3.4 AI Microservice

The AI service runs locally using:

- **Python**
- **FastAPI**
- **YOLOv8**

Its role is to detect manga panels and return coordinates in JSON format for the main system.

### 3.5 Database & Storage

- **SQL Server** stores core business data
- **Cloudinary** stores and delivers large manga image assets

---

## 4. System Modules

### 4.1 Identity & Governance Module
Handles:

- authentication,
- authorization,
- user profiles,
- portfolios,
- income wallet/data,
- system settings.

Core purpose: ensure data is only accessible to the correct user and role.

### 4.2 Studio Production Module
Handles:

- interactive canvas,
- task orchestration,
- page segmentation,
- asset versioning.

Core purpose: turn static manga pages into an interactive production workspace.

### 4.3 AI Intelligence Hub
Handles:

- object detection,
- panel segmentation,
- AI-to-.NET JSON bridge.

Core purpose: reduce manual work through AI assistance.

### 4.4 Publishing & Editorial Module
Handles:

- annotations and markers,
- review workflow,
- publishing states,
- webtoon-style reader/editor view.

Core purpose: ensure quality before publication.

### 4.5 Business Analytics & Ranking Module
Handles:

- vote collection,
- ranking computation,
- monitoring and alerts.

Core purpose: support editorial and business decisions.

## 5. Technology Stack
Layer	Technology
Frontend	Blazor Server
Admin UI	MudBlazor
Canvas	Fabric.js
Backend	ASP.NET Core Web API
Architecture	Clean Architecture
AI Microservice	Python FastAPI
AI Model	YOLOv8
Database	SQL Server
ORM	Entity Framework Core
Storage	Cloudinary
Real-time	SignalR
Version Control	GitHub
Project Tracking	Jira
Documentation	Confluence