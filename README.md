# MangaManagementSystem
1. Project Overview

Mangaka Platform is a multi-module software engineering project designed to support the full production pipeline of manga/webtoon creation, review, publishing, and analytics.

The system focuses on:

collaborative studio workflows,
AI-assisted automation,
editorial quality control,
production task orchestration,
real-time notifications,
analytics and ranking systems.
2. Current Repository Status

PRIVATE DEVELOPMENT REPOSITORY

This repository is currently under active development and intended only for internal team collaboration.

The codebase may contain:

unfinished features,
unstable APIs,
experimental implementations,
incomplete documentation.
3. System Modules
3.1 Identity & Governance Module

Handles:

authentication,
authorization,
role management,
user profiles,
governance and system settings.
Roles
Administrator
Mangaka
Assistant
Editor
3.2 Studio Production Module

Core production workspace for manga creation.

Features:

page upload,
interactive Fabric.js canvas,
panel region selection,
task orchestration,
asset versioning.
3.3 AI Intelligence Hub

AI-assisted automation services.

Features:

YOLOv8 panel detection,
FastAPI inference service,
JSON API bridge for .NET integration.
3.4 Publishing & Editorial Module

Editorial review and publishing workflow.

Features:

annotation markers,
review workflow,
approval states,
webtoon-style reader.
3.5 Business Analytics & Ranking Module

Business intelligence and monitoring layer.

Features:

vote collection,
ranking algorithms,
analytics dashboard,
monitoring alerts.
3.6 Infrastructure & QA Module

Shared infrastructure and quality assurance systems.

Features:

Cloudinary integration,
SignalR notifications,
automated testing,
CI/CD support.
4. Technology Stack
Layer	Technology
Backend	ASP.NET Core (.NET)
Frontend	JavaScript / HTML / CSS
Canvas Engine	Fabric.js
AI Service	Python + FastAPI + YOLOv8
Real-time	SignalR
Database	SQL Server
Charts	Chart.js
Image Storage	Cloudinary
Version Control	GitHub
Project Management	Jira
Documentation	Confluence