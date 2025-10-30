# Phase 5 Data Migration Scripts

This folder contains SQL scripts for migrating data from the legacy system to the new microservices schemas.

- 001-migrate-users.sql
- 002-migrate-conversations.sql
- 003-migrate-tasks.sql

Run order matters. These scripts are idempotent where possible.
