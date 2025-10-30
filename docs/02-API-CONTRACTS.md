# API Contracts - OpenAPI Index

Status: Draft
Version: 1.0.0
Last Updated: October 24, 2025

---

## Purpose

Central index for OpenAPI specifications for all services. Each service must provide a machine-readable spec and keep it in sync with implementation.

- Style: OpenAPI 3.1
- Naming: kebab-case filenames
- Location: docs/api/

## Specs

- Gateway: [api/gateway-openapi.yaml](api/gateway-openapi.yaml)
- Chat Service: [api/chat-service-openapi.yaml](api/chat-service-openapi.yaml)
- Orchestration Service: docs/api/orchestration-service-openapi.yaml (TBD)
- ML Classifier: docs/api/ml-classifier-openapi.yaml (TBD)
- GitHub Service: docs/api/github-service-openapi.yaml (TBD)
- Browser Service: docs/api/browser-service-openapi.yaml (TBD)
- CI/CD Monitor: docs/api/cicd-monitor-openapi.yaml (TBD)
- Dashboard Service: docs/api/dashboard-service-openapi.yaml (TBD)

## Requirements

- Contract-first: update spec before implementation
- Backward compatible changes only on minor versions
- Breaking changes require major version and migration notes
- Include examples for all endpoints

## Auth Service Admin Endpoints

### User Management API

**Base Path**: `/api/auth/admin`
**Authorization**: Requires JWT with `Admin` role claim

#### GET /admin/users
Retrieve paginated list of users with optional filtering.

**Query Parameters**:
- `page` (int): Page number (default: 1, min: 1)
- `pageSize` (int): Items per page (default: 20, range: 1-100)
- `search` (string, optional): Search username or email
- `role` (string, optional): Filter by role

**Response 200**:
```json
{
  "users": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "username": "admin",
      "email": "admin@example.com",
      "roles": ["Admin", "User"],
      "isActive": true,
      "createdAt": "2025-10-27T10:00:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

#### GET /admin/users/{id}
Get detailed information about a specific user.

**Response 200**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "johndoe",
  "email": "john.doe@example.com",
  "roles": ["User"],
  "isActive": true,
  "createdAt": "2025-10-27T10:00:00Z",
  "updatedAt": "2025-10-27T14:30:00Z",
  "sessionCount": 3
}
```

#### PUT /admin/users/{id}/roles
Update user roles.

**Request Body**:
```json
{
  "roles": ["Admin", "User"]
}
```

**Response**: 204 No Content

**Safety Checks**:
- Cannot remove Admin role from own account
- Cannot remove Admin role from last active administrator

#### PUT /admin/users/{id}/activate
Activate a deactivated user account.

**Response**: 204 No Content

#### PUT /admin/users/{id}/deactivate
Deactivate a user account (prevents login).

**Response**: 204 No Content

**Safety Checks**:
- Cannot deactivate own account
- Cannot deactivate last active administrator

## Next Steps

- [ ] Create initial Gateway OpenAPI (routes + health/metrics)
- [ ] Add Chat Service endpoints
	- Presence: GET /presence/{conversationId}
		- Description: Returns presence for participants in the conversation
		- Response: [{ userId: string (GUID), isOnline: boolean, lastSeenUtc: string|null }]
		- Notes: Participants approximated from recent messages until participant list is available
- [ ] Add Orchestration Service endpoints
- [ ] Add ML Classifier endpoints
- [ ] Wire CI to validate OpenAPI (spectral)
