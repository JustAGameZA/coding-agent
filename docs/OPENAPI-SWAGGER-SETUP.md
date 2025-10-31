# OpenAPI / Swagger Setup Guide

Guide for accessing and using OpenAPI documentation (Swagger UI) across all services.

## Overview

All services expose OpenAPI/Swagger documentation for API exploration and testing.

## Service Endpoints

| Service | Swagger UI URL | OpenAPI JSON |
|---------|----------------|--------------|
| Gateway | http://localhost:5000/swagger | http://localhost:5000/swagger/v1/swagger.json |
| Auth Service | http://localhost:5001/swagger | http://localhost:5001/swagger/v1/swagger.json |
| Chat Service | http://localhost:5002/swagger | http://localhost:5002/swagger/v1/swagger.json |
| Orchestration Service | http://localhost:5003/swagger | http://localhost:5003/swagger/v1/swagger.json |
| GitHub Service | http://localhost:5004/swagger | http://localhost:5004/swagger/v1/swagger.json |
| Browser Service | http://localhost:5005/swagger | http://localhost:5005/swagger/v1/swagger.json |
| CI/CD Monitor | http://localhost:5006/swagger | http://localhost:5006/swagger/v1/swagger.json |
| Dashboard Service | http://localhost:5007/swagger | http://localhost:5007/swagger/v1/swagger.json |

## Accessing Swagger UI

### Via Browser

1. Ensure services are running (via Docker Compose or locally)
2. Navigate to service Swagger URL (e.g., http://localhost:5000/swagger)
3. Explore available endpoints
4. Test endpoints directly from Swagger UI

### Authentication in Swagger

To test authenticated endpoints:

1. Use the `/auth/login` endpoint to get a JWT token
2. Click "Authorize" button in Swagger UI
3. Enter token as: `Bearer <your-token>`
4. Click "Authorize" to save
5. All subsequent requests will include the token

## OpenAPI Specifications

OpenAPI specifications are also available as YAML files in `docs/api/`:

- `gateway-openapi.yaml` - Gateway API specification
- `auth-service-openapi.yaml` - Auth service specification
- `chat-service-openapi.yaml` - Chat service specification

### Generating Client Code

Use OpenAPI Generator to generate client SDKs:

```bash
# Install OpenAPI Generator
npm install -g @openapitools/openapi-generator-cli

# Generate TypeScript client
openapi-generator-cli generate \
  -i docs/api/chat-service-openapi.yaml \
  -g typescript-axios \
  -o src/generated/chat-client

# Generate C# client
openapi-generator-cli generate \
  -i docs/api/auth-service-openapi.yaml \
  -g csharp-netcore \
  -o src/generated/auth-client
```

## Swagger Configuration

### Enabling/Disabling Swagger

Swagger is enabled by default in Development, disabled in Production.

To manually configure, check `Program.cs` in each service:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### Customizing Swagger

Add OpenAPI metadata in endpoint definitions:

```csharp
app.MapGet("/endpoint", Handler)
    .WithName("EndpointName")
    .WithTags("TagName")
    .WithSummary("Brief description")
    .WithDescription("Detailed description")
    .Produces<ResponseDto>(StatusCodes.Status200OK)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest);
```

## Verification Checklist

- [ ] All services expose `/swagger` endpoint
- [ ] Swagger UI loads correctly in browser
- [ ] All endpoints are documented
- [ ] Authentication works via Swagger UI
- [ ] OpenAPI JSON is valid
- [ ] Client code generation works

## Troubleshooting

### Swagger Not Loading

1. Check service is running: `docker ps | grep <service>`
2. Verify Swagger is enabled for environment
3. Check service logs for errors
4. Ensure port is accessible: `curl http://localhost:<port>/swagger`

### Authentication Issues

1. Verify JWT token is valid
2. Check token format: `Bearer <token>`
3. Ensure token hasn't expired
4. Verify service JWT configuration matches token issuer/audience

---

**Last Updated**: December 2025

