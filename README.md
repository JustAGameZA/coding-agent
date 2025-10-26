# 🤖 Coding Agent - AI-Powered Microservices Platform

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular 20](https://img.shields.io/badge/Angular-20.3-DD0031?logo=angular)](https://angular.io/)
[![Python](https://img.shields.io/badge/Python-3.11+-3776AB?logo=python)](https://www.python.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> **An enterprise-grade AI coding assistant built with microservices architecture**

A sophisticated coding assistant platform that combines real-time chat, task orchestration, ML-powered classification, and automated GitHub operations—all built with modern microservices principles.

---

## 🌟 Features

- **💬 Real-time Chat** - SignalR-powered WebSocket communication
- **🤖 AI Task Orchestration** - Intelligent agent coordination and execution
- **🧠 ML Classification** - Hybrid heuristic + ML task categorization
- **🔧 GitHub Integration** - Automated repository operations and PR management
- **🌐 Browser Automation** - Playwright-powered web interaction
- **📊 CI/CD Monitoring** - Build tracking and automated fixes
- **📈 Observability** - OpenTelemetry with Prometheus, Grafana, and Jaeger
- **🚀 Scalable Architecture** - Independent service deployment and scaling

---

## 🏗️ Architecture

### Microservices (8 Services)

```mermaid
graph TB
    Client[Angular Dashboard] --> Gateway[API Gateway - YARP]
    Gateway --> Chat[Chat Service]
    Gateway --> Orch[Orchestration Service]
    Gateway --> GitHub[GitHub Service]
    Gateway --> Browser[Browser Service]
    Gateway --> CICD[CI/CD Monitor]
    Gateway --> Dashboard[Dashboard BFF]

    Orch --> ML[ML Classifier - Python]

    Chat --> RabbitMQ[(RabbitMQ)]
    Orch --> RabbitMQ
    GitHub --> RabbitMQ

    Chat --> Postgres[(PostgreSQL)]
    Orch --> Postgres
    GitHub --> Postgres

    Chat --> Redis[(Redis Cache)]
    Orch --> Redis
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 9 Minimal APIs, Python FastAPI |
| **Frontend** | Angular 20.3 + NgRx Signal Store |
| **Gateway** | YARP (Yet Another Reverse Proxy) |
| **Messaging** | RabbitMQ + MassTransit |
| **Database** | PostgreSQL (per-service schemas) |
| **Cache** | Redis |
| **Real-time** | SignalR (WebSocket) |
| **Observability** | OpenTelemetry → Prometheus + Grafana + Jaeger |
| **Deployment** | Docker Compose (dev), Kubernetes (prod) |

---

## 📚 Documentation

Comprehensive documentation is available in the [`docs/`](./docs) directory:

### Architecture Guides
- **[📖 Overview](./docs/00-OVERVIEW.md)** - System architecture and design decisions
- **[📋 Service Catalog](./docs/01-SERVICE-CATALOG.md)** - Detailed service specifications
- **[🗓️ Roadmap](./docs/02-IMPLEMENTATION-ROADMAP.md)** - 6-month implementation plan
- **[📁 Solution Structure](./docs/03-SOLUTION-STRUCTURE.md)** - Monorepo layout and CI/CD
- **[🧠 ML & Orchestration ADR](./docs/04-ML-AND-ORCHESTRATION-ADR.md)** - ML architecture decisions
- **[⚡ Quick Start](./docs/QUICK-START.md)** - Quick reference guide

### Deployment & Operations
- **[🐳 Docker Guide](./deployment/docker-compose/README.md)** - Complete Docker setup (dev + prod)
- **[⚡ Docker Quick Start](./deployment/docker-compose/DOCKER-QUICK-START.md)** - Common commands
- **[📦 Docker Implementation](./deployment/docker-compose/DOCKER-IMPLEMENTATION-SUMMARY.md)** - Technical details
- **[🚨 Alerting Setup](./deployment/docker-compose/ALERTING-SUMMARY.md)** - Monitoring & alerts
- **[📖 Runbooks](./docs/runbooks/)** - Operational procedures

### API Documentation
- API specifications will be available in `docs/api/` (Phase 1)
- OpenAPI/Swagger endpoints for each service

### Contributing
- **[Contributing Guide](./.github/CONTRIBUTING.md)** - How to contribute
- **[Code of Conduct](./.github/CODE_OF_CONDUCT.md)** - Community guidelines
- **[Security Policy](./.github/SECURITY.md)** - Reporting vulnerabilities
- **[Copilot Guide](./.github/COPILOT.md)** - GitHub Copilot best practices

---

## 🚀 Quick Start

### Prerequisites

- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **Python 3.11+** - [Download](https://www.python.org/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/)

### Clone & Setup

```bash
# Clone the repository
git clone https://github.com/JustAGameZA/coding-agent.git
cd coding-agent
```

### Run with Docker (Recommended)

**Development Mode** (with hot reload):
```bash
cd deployment/docker-compose

# Start infrastructure + all services
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up

# Access services:
# - Gateway API: http://localhost:5000
# - Angular UI: http://localhost:4200
# - Grafana: http://localhost:3000 (admin/admin)
# - Jaeger: http://localhost:16686
```

**Production Mode** (optimized builds):
```bash
cd deployment/docker-compose

# Build and start all services
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up --build -d

# Check status
docker compose ps
```

**Documentation:**
- 📚 [Complete Docker Guide](./deployment/docker-compose/README.md)
- ⚡ [Quick Start Commands](./deployment/docker-compose/DOCKER-QUICK-START.md)
- 🛠️ [Implementation Details](./deployment/docker-compose/DOCKER-IMPLEMENTATION-SUMMARY.md)

### Development Roadmap

**Current Status**: ✅ Phase 3 Complete (Integration Services)
**Current Phase**: Phase 4 - Frontend & Dashboard (Starting)

| Phase | Timeline | Status |
|-------|----------|--------|
| Phase 0: Architecture & Planning | Weeks 1-2 | ✅ Complete |
| Phase 1: Infrastructure & Gateway | Weeks 3-6 | ✅ Complete |
| Phase 2: Core Services | Weeks 7-12 | ✅ Complete |
| Phase 3: Integration Services | Weeks 13-18 | ✅ Complete |
| Phase 4: Frontend & Dashboard | Weeks 19-22 | ⏳ In Progress |
| Phase 5: Migration & Cutover | Weeks 23-24 | 📋 Planned |
| Phase 6: Stabilization & Docs | Weeks 25-26 | 📋 Planned |

**Progress**: 67% complete (4 of 6 phases done)
**Target Completion**: May 2026

---

## 🎯 Project Goals

### Technical Metrics
- ✅ **API Latency**: p95 < 500ms
- ✅ **Test Coverage**: 85%+ (unit + integration)
- ✅ **Build Time**: < 5 min per service
- ✅ **Deployment**: Daily per service
- ✅ **Availability**: 99.5%+ uptime

### Business Impact
- 🎯 **Feature Velocity**: 2x faster development
- 🎯 **Onboarding**: < 1 day for new developers
- 🎯 **Incident Recovery**: < 5 min
- 🎯 **Cost Efficiency**: 30% reduction via independent scaling

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](./.github/CONTRIBUTING.md) for details.

### Development Workflow
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📋 Project Status

### ✅ Completed (Phases 0-3)

**Phase 0: Architecture & Planning**
- ✅ Complete service catalog and specifications (78 KB documentation)
- ✅ Solution structure with 16 projects and 8 services
- ✅ Architecture Decision Records (ADRs)

**Phase 1: Infrastructure & Gateway**
- ✅ YARP Gateway with JWT auth and rate limiting
- ✅ PostgreSQL + Redis + RabbitMQ + Ollama in Docker Compose
- ✅ OpenTelemetry → Prometheus + Grafana + Jaeger observability stack
- ✅ Automated alerting and health monitoring

**Phase 2: Core Services**
- ✅ Chat Service (141 unit tests, SignalR hub, Redis caching)
- ✅ Orchestration Service (214 unit tests, 3 execution strategies)
- ✅ ML Classifier Service (145 tests, hybrid heuristic+ML+LLM)

**Phase 3: Integration Services**
- ✅ GitHub Service (57 tests, PR management + webhooks)
- ✅ Browser Service (Playwright automation)
- ✅ CI/CD Monitor (automated fix generation)
- ✅ Ollama Service (hardware-aware model selection)

**Total Test Coverage**: 532+ unit tests across all services

### 🚧 In Progress (Phase 4)

- Dashboard Service (BFF) - minimal scaffold with observability
- Angular Dashboard - basic routing + Material UI setup
- Service integration and E2E testing

### 📋 Upcoming (Phases 5-6)
- Full infrastructure stack (Phase 1)
- Core service implementation (Phase 2)
- Frontend dashboard (Phase 4)

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

This project was designed with assistance from:
- **GitHub Copilot** - AI-assisted architecture and development
- **Microsoft eShopOnContainers** - Microservices inspiration
- **Clean Architecture** - Domain-driven design principles
- **.NET Community** - Best practices and patterns

---

## 📞 Support & Community

- **Documentation**: [docs/](./docs)
- **Issues**: [GitHub Issues](https://github.com/zerith-jag/coding-agent/issues)
- **Discussions**: [GitHub Discussions](https://github.com/zerith-jag/coding-agent/discussions)

---

## 🔗 Related Links

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Angular Documentation](https://angular.io/docs)
- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)

---

<p align="center">
  <strong>🎯 Building the Future of AI Coding Assistants</strong>
</p>

<p align="center">
  Made with ❤️ by <a href="https://github.com/zerith-jag">zerith-jag</a>
</p>

<p align="center">
  <sub>Last Updated: October 24, 2025</sub>
</p>
