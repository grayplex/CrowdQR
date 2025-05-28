# 🎧 CrowdQR – Real-Time DJ Audience Interaction App

[![CI/CD Pipeline](https://github.com/grayplex/crowdqr/actions/workflows/ci.yml/badge.svg)](https://github.com/grayplex/crowdqr/actions/workflows/ci.yml)
![Code Coverage](https://img.shields.io/codecov/c/github/grayplex/CrowdQR)
[![CodeFactor](https://www.codefactor.io/repository/github/grayplex/crowdqr/badge)](https://www.codefactor.io/repository/github/grayplex/crowdqr)
[![Release](https://img.shields.io/github/v/release/grayplex/crowdqr)](https://github.com/grayplex/crowdqr/releases)
[![License](https://img.shields.io/github/license/grayplex/crowdqr)](LICENSE)
[![Docker](https://img.shields.io/badge/docker-ready-blue?logo=docker)](https://github.com/grayplex/crowdqr/pkgs/container/crowdqr)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)

CrowdQR is a web-based platform that allows live audiences to engage with DJs during performances by scanning a QR code to submit and vote on song requests in real time. DJs can manage requests through a secure admin interface, allowing them to curate sets dynamically based on audience feedback.

This app is built as part of a Software Engineering Capstone project and is designed to demonstrate full-stack development, real-time systems, and containerized deployment.

---

## 🎯 Project Vision & Goals

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

## 📂 Project Structure (WIP)

```
CrowdQR/
├── Backend/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   ├── Hubs/
│   └── Program.cs
├── Frontend/
│   ├── Pages/
│   ├── Shared/
│   └── wwwroot/
├── docs/
│   ├── wireframes/
│   └── erd/
│       └── redis-schema.md
├── docker-compose.yml
├── Dockerfile
├── README.md
├── .gitignore
└── LICENSE
```

---

## 📃 License

MIT License – see `LICENSE` file for details.
