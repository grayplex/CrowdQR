# 🎧 CrowdQR – Real-Time DJ Audience Interaction App

[![CI/CD Pipeline](https://github.com/grayplex/crowdqr/actions/workflows/ci.yml/badge.svg)](https://github.com/grayplex/crowdqr/actions/workflows/ci.yml)
[![Release Pipeline](https://github.com/grayplex/CrowdQR/actions/workflows/release.yml/badge.svg)](https://github.com/grayplex/CrowdQR/actions/workflows/release.yml)
![Code Coverage](https://img.shields.io/codecov/c/github/grayplex/CrowdQR)
[![CodeFactor](https://www.codefactor.io/repository/github/grayplex/crowdqr/badge)](https://www.codefactor.io/repository/github/grayplex/crowdqr)

[![Release](https://img.shields.io/github/v/release/grayplex/crowdqr)](https://github.com/grayplex/crowdqr/releases)
[![License](https://img.shields.io/github/license/grayplex/crowdqr)](LICENSE)
[![Docker](https://img.shields.io/badge/docker-ready-blue?logo=docker)](https://github.com/grayplex/crowdqr/pkgs/container/crowdqr)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)

CrowdQR is a web-based platform that allows live audiences to engage with DJs during performances by scanning a QR code to submit and vote on song requests in real time. DJs can manage requests through a secure admin interface, allowing them to curate sets dynamically based on audience feedback.

This app is built as part of a Software Engineering Capstone project and is designed to demonstrate full-stack development, real-time systems, and containerized deployment.

---

## 📋 Quick Start

### Prerequisites

- Docker 20.10+ and Docker Compose 2.0+
- 2GB+ RAM available  
- Ports 5000, 8080, 5433 available

### 1-Minute Deployment

```bash
# Clone the repository
git clone https://github.com/grayplex/crowdqr.git
cd crowdqr

# Copy and configure environment
cp .env.example .env
# Edit .env file - CHANGE the default passwords!

# Deploy with Docker Compose
docker-compose up -d

# Verify deployment
curl http://localhost:5000/health
curl http://localhost:8080/health
```

### 🌐 Access the Application

- Web App: <http://localhost:8080>
- API: <http://localhost:5000>
- Database: localhost:5433

---

## 📖 Documentation

### 🏗️ Architecture & Design

- [System Architecture](docs/ARCHITECTURE.md) - Complete architecture diagrams and data flow
- [API Documentation](src/CrowdQR.Api) - Backend API reference
- [Web Application](src/CrowdQR.Web) - Frontend application
- [Shared Components](src/CrowdQR.Shared/) - Shared models and DTOs

### 🚀 Deployment & Operations

- [Deployment Guide](deploy/README.md) - Complete production deployment instructions
- [Environment Variables](docs/ENVIRONMENT_VARIABLES.md) - Configuration reference
- [Command Reference](docs/DEPLOYMENT_COMMANDS.md) - Build, run, and troubleshoot commands
- [Automated Testing](deploy/production-test.sh) - Production deployment testing script

### 🧪 Testing & Quality Assurance

- [E2E Test Workflow](tests/EndToEndTestWorkflow.md) - Step-by-step testing guide
- [Testing Checklist](tests/TestingChecklist.md) - Comprehensive testing scenarios
- [Test Coverage](tests/CrowdQR.Api.Tests/README.md) - Unit and integration test documentation

### 🔧 Development & Contributing

- **[Release Scripts](scripts/README.md)** - Docker image tagging and release automation
- **[CI/CD Pipeline](.github/workflows/ci.yml)** - Automated testing and deployment
- **[Project Structure](#project-structure)** - Codebase organization and conventions

## 🎯 Project Vision & Goal

### ✅ Vision

Empower independent DJs and small venue performers to create more interactive, engaging sets by digitizing audience participation — no shouting requests, no pen-and-paper lists.

### ✅ Core Goals

- Provide a mobile-friendly interface for users to submit and vote on song requests
- Equip DJs with a live dashboard to approve/reject requests
- Enable real-time updates without page refreshes
- Prevent spam, duplicate votes, and request flooding via backend logic
- Support event-based session tracking and persistent request queues
- Deploy everything inside Docker containers for portability

---

## 📌 Use Cases

- 🎧 **DJ Admin** logs into the dashboard and sees live requests and vote counts
- 👥 **Audience Member** scans a QR code, types a song name, and votes on others' requests
- 🔄 Requests are approved/rejected by the DJ and synced in real time
- 🔒 Voting is limited per user/session to prevent abuse

---

## 📖 How to Use CrowdQR

### 🎧 For DJs

1. **Register as a DJ**
   - Visit the web application at `http://localhost:8080`
   - Click "Register" and check "Register as a DJ"
   - Complete registration with username, email, and password
   - Verify your email (check console logs in development mode)

2. **Create an Event**
   - Log in to your DJ account
   - Navigate to Admin → Events
   - Click "Create New Event"
   - Enter event name and URL-friendly slug (e.g., "saturday-night-fever")
   - Set event as active

3. **Generate QR Code**
   - From the Events page, click the QR Code button for your event
   - Display the QR code on screens or print it for your venue
   - The QR code contains the URL: `http://localhost:8080/event/your-event-slug`

4. **Manage Requests**
   - Go to Admin → Dashboard and select your event
   - View pending requests sorted by vote count
   - Approve ✅ or reject ❌ requests in real-time
   - See live updates as audience members vote
   - Switch between Pending, Approved, and Rejected tabs

5. **Monitor Activity**
   - View active user count and total requests
   - Search requests by song, artist, or requester name
   - Track request statistics and popular songs

### 👥 For Audience Members

1. **Join an Event**
   - Scan the QR code displayed at the venue, OR
   - Visit the web app and enter the event code
   - A temporary username will be created automatically

2. **Request Songs**
   - Enter the song name (required)
   - Add artist name (optional but recommended)
   - Click "Submit Request"
   - Your request appears in the live list immediately

3. **Vote on Requests**
   - Browse all pending song requests
   - Click the 👍 button to vote for songs you want to hear
   - Click again to remove your vote
   - Requests are sorted by vote count (highest first)

4. **Real-Time Updates**
   - Watch the list update automatically as others vote
   - See when DJs approve ✅ or reject ❌ requests
   - Connection status indicator shows if you're connected to live updates

### 🔄 Real-Time Features

- **Live Voting**: Vote counts update instantly across all devices
- **Status Updates**: See approvals/rejections in real-time
- **Active Users**: DJs can see how many audience members are connected
- **Auto-Refresh**: No need to refresh the page - everything updates automatically

### 🎯 Tips for Best Results

**For DJs:**

- Keep events active during your set for audience engagement
- Regularly check the dashboard during performances
- Use the search feature to find specific requests quickly
- Consider approving popular requests to keep the audience engaged

**For Audience:**

- Be specific with song names and artists for better recognition
- Vote for requests you genuinely want to hear
- Check back periodically to see if your requests were approved
- Respect the DJ's musical direction and choices

### 🚨 Troubleshooting

- **Can't connect?** Check that ports 5000 and 8080 are accessible
- **QR code not working?** Ensure the event slug is correct and the event is active
- **Requests not appearing?** Verify your internet connection and refresh the page
- **Vote not counting?** You may have already voted for that request

---

## 🧱 Tech Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | ASP.NET Razor Pages / Blazor (WebAssembly optional) |
| **Backend** | ASP.NET Core Web API |
| **Database** | PostgreSQL (via EF Core) |
| **Real-Time** | SignalR |
| **Authentication** | Session-based or JWT |
| **Caching/Rate Limiting** | Redis (optional) |
| **Containerization** | Docker + Docker Compose |
| **CI/CD (optional)** | GitHub Actions (for build/test) |

---

## 🗺 Roadmap

### ✅ MVP Goals (Capstone Completion)

- [ ] Submit & vote on song requests
- [ ] Admin dashboard with approval queue
- [ ] Role-based access (DJ vs Audience)
- [ ] SignalR for live updates
- [ ] Rate-limiting and vote protections
- [ ] Dockerized full-stack deployment

### 💡 Planned Enhancements (Post-MVP)

- [ ] Spotify/YouTube metadata enrichment
- [ ] OAuth login (Google/Spotify)
- [ ] DJ analytics dashboard (popular songs, request volume)
- [ ] Event switching / multi-room support
- [ ] "My Activity" page for users
- [ ] Mobile PWA install option

---

## Project Structure

```plaintext
CrowdQR/
├── .github/
│   └── workflows/
├── deploy/
├── docs/
│   ├── api/
│   ├── erd/
│   └── wireframes/
├── scripts/
│   ├── README.md
│   ├── run-tests.ps1
│   ├── tag-release.ps1
│   └── tag-release.sh
├── src/
│   ├── CrowdQR.Api/
|   │   ├── Controllers/
|   │   ├── Data/
|   │   ├── Hubs/
|   │   ├── Middleware/
|   │   ├── Migrations/
|   │   ├── Models
|   │   ├── Properties/
|   │   ├── Services/
|   │   ├── appsettings.json
|   │   ├── CrowdQR.Api.csproj
|   │   ├── Dockerfile
|   │   └── Program.cs
│   ├── CrowdQR.Shared/
|   │   ├── Models/
|   |   │   ├── DTOs/
|   │   │   └── Enums/
|   │   └── CrowdQR.Shared.csproj
│   └── CrowdQR.Web/
|       ├── Extensions/
|       ├── Pages/
|       |   ├── Admin/
|       |   └── Shared/
|       ├── Properties/
|       ├── Services/
|       ├── Utilities/
|       ├── wwwroot/
|       ├── appsettings.json
|       ├── CrowdQR.Web.csproj
|       ├── Dockerfile
|       └── Program.cs
├── tests/
│   └── CrowdQR.Api.Tests/
|       ├── Controllers/
|       ├── Helpers/
|       ├── Integration/
|       ├── Middleware/
|       ├── Services/
│       └── Validation/
├── .env.example
├── .gitignore
├── CrowdQR.sln
├── Directory.Build.props
├── docker-compose.yml
├── LICENSE
└── README.md
```

---

## 📃 License

MIT License – see `LICENSE` file for details.
