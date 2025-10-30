-- ============================================
-- PostgreSQL Database Initialization
-- Coding Agent - Microservices Platform
-- ============================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- ==========================================
-- Schema: chat
-- Service: Chat Service
-- Purpose: Conversations, messages, attachments
-- ==========================================
CREATE SCHEMA IF NOT EXISTS chat;

-- Conversations table
CREATE TABLE IF NOT EXISTS chat.conversations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(255) NOT NULL,
    title VARCHAR(500),
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Messages table
CREATE TABLE IF NOT EXISTS chat.messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    conversation_id UUID NOT NULL REFERENCES chat.conversations(id) ON DELETE CASCADE,
    sender VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    message_type VARCHAR(50) NOT NULL DEFAULT 'text',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Attachments table
CREATE TABLE IF NOT EXISTS chat.attachments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    message_id UUID NOT NULL REFERENCES chat.messages(id) ON DELETE CASCADE,
    file_name VARCHAR(500) NOT NULL,
    file_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    storage_path VARCHAR(1000) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for chat schema
CREATE INDEX IF NOT EXISTS idx_conversations_user_id ON chat.conversations(user_id);
CREATE INDEX IF NOT EXISTS idx_conversations_created_at ON chat.conversations(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_messages_conversation_id ON chat.messages(conversation_id);
CREATE INDEX IF NOT EXISTS idx_messages_created_at ON chat.messages(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_attachments_message_id ON chat.attachments(message_id);

-- ==========================================
-- Schema: orchestration
-- Service: Orchestration Service
-- Purpose: Tasks, executions, results
-- ==========================================
CREATE SCHEMA IF NOT EXISTS orchestration;

-- Coding tasks table
CREATE TABLE IF NOT EXISTS orchestration.coding_tasks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    conversation_id UUID NOT NULL,
    task_type VARCHAR(100) NOT NULL,
    complexity VARCHAR(50) NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    priority INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB
);

-- Task executions table
CREATE TABLE IF NOT EXISTS orchestration.task_executions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    task_id UUID NOT NULL REFERENCES orchestration.coding_tasks(id) ON DELETE CASCADE,
    execution_strategy VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    started_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP WITH TIME ZONE,
    error_message TEXT,
    metadata JSONB
);

-- Execution results table
CREATE TABLE IF NOT EXISTS orchestration.execution_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    execution_id UUID NOT NULL REFERENCES orchestration.task_executions(id) ON DELETE CASCADE,
    result_type VARCHAR(100) NOT NULL,
    result_data JSONB NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for orchestration schema
CREATE INDEX IF NOT EXISTS idx_tasks_conversation_id ON orchestration.coding_tasks(conversation_id);
CREATE INDEX IF NOT EXISTS idx_tasks_status ON orchestration.coding_tasks(status);
CREATE INDEX IF NOT EXISTS idx_tasks_created_at ON orchestration.coding_tasks(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_executions_task_id ON orchestration.task_executions(task_id);
CREATE INDEX IF NOT EXISTS idx_executions_status ON orchestration.task_executions(status);
CREATE INDEX IF NOT EXISTS idx_results_execution_id ON orchestration.execution_results(execution_id);

-- ==========================================
-- Schema: github
-- Service: GitHub Service
-- Purpose: Repositories, pull requests, issues
-- ==========================================
CREATE SCHEMA IF NOT EXISTS github;

-- Repositories table
CREATE TABLE IF NOT EXISTS github.repositories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    github_id BIGINT UNIQUE NOT NULL,
    owner VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    full_name VARCHAR(512) NOT NULL,
    description TEXT,
    url VARCHAR(1000) NOT NULL,
    default_branch VARCHAR(255) NOT NULL DEFAULT 'main',
    is_private BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Pull requests table
CREATE TABLE IF NOT EXISTS github.pull_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    repository_id UUID NOT NULL REFERENCES github.repositories(id) ON DELETE CASCADE,
    github_id BIGINT NOT NULL,
    number INTEGER NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    state VARCHAR(50) NOT NULL,
    head_ref VARCHAR(255) NOT NULL,
    base_ref VARCHAR(255) NOT NULL,
    author VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    merged_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB,
    UNIQUE(repository_id, number)
);

-- Issues table
CREATE TABLE IF NOT EXISTS github.issues (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    repository_id UUID NOT NULL REFERENCES github.repositories(id) ON DELETE CASCADE,
    github_id BIGINT NOT NULL,
    number INTEGER NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    state VARCHAR(50) NOT NULL,
    author VARCHAR(255) NOT NULL,
    assignee VARCHAR(255),
    labels TEXT[],
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    closed_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB,
    UNIQUE(repository_id, number)
);

-- Create indexes for github schema
CREATE INDEX IF NOT EXISTS idx_repositories_github_id ON github.repositories(github_id);
CREATE INDEX IF NOT EXISTS idx_repositories_full_name ON github.repositories(full_name);
CREATE INDEX IF NOT EXISTS idx_pull_requests_repository_id ON github.pull_requests(repository_id);
CREATE INDEX IF NOT EXISTS idx_pull_requests_state ON github.pull_requests(state);
CREATE INDEX IF NOT EXISTS idx_issues_repository_id ON github.issues(repository_id);
CREATE INDEX IF NOT EXISTS idx_issues_state ON github.issues(state);

-- ==========================================
-- Schema: cicd
-- Service: CI/CD Monitor
-- Purpose: Builds, workflows, deployments
-- ==========================================
CREATE SCHEMA IF NOT EXISTS cicd;

-- Workflow runs table
CREATE TABLE IF NOT EXISTS cicd.workflow_runs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    repository_id UUID NOT NULL,
    github_run_id BIGINT UNIQUE NOT NULL,
    workflow_name VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL,
    conclusion VARCHAR(50),
    branch VARCHAR(255) NOT NULL,
    commit_sha VARCHAR(40) NOT NULL,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,
    duration_seconds INTEGER,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Build jobs table
CREATE TABLE IF NOT EXISTS cicd.build_jobs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workflow_run_id UUID NOT NULL REFERENCES cicd.workflow_runs(id) ON DELETE CASCADE,
    github_job_id BIGINT NOT NULL,
    job_name VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL,
    conclusion VARCHAR(50),
    started_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,
    duration_seconds INTEGER,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Deployments table
CREATE TABLE IF NOT EXISTS cicd.deployments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workflow_run_id UUID NOT NULL REFERENCES cicd.workflow_runs(id) ON DELETE CASCADE,
    environment VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL,
    deployed_by VARCHAR(255) NOT NULL,
    deployed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    version VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Create indexes for cicd schema
CREATE INDEX IF NOT EXISTS idx_workflow_runs_repository_id ON cicd.workflow_runs(repository_id);
CREATE INDEX IF NOT EXISTS idx_workflow_runs_status ON cicd.workflow_runs(status);
CREATE INDEX IF NOT EXISTS idx_workflow_runs_started_at ON cicd.workflow_runs(started_at DESC);
CREATE INDEX IF NOT EXISTS idx_build_jobs_workflow_run_id ON cicd.build_jobs(workflow_run_id);
CREATE INDEX IF NOT EXISTS idx_build_jobs_status ON cicd.build_jobs(status);
CREATE INDEX IF NOT EXISTS idx_deployments_workflow_run_id ON cicd.deployments(workflow_run_id);
CREATE INDEX IF NOT EXISTS idx_deployments_environment ON cicd.deployments(environment);

-- ==========================================
-- Schema: auth
-- Service: Auth Service
-- Purpose: Users, sessions, API keys, roles, permissions
-- ==========================================
CREATE SCHEMA IF NOT EXISTS auth;

-- Users table
CREATE TABLE IF NOT EXISTS auth.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(500) NOT NULL,
    full_name VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_verified BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB
);

-- Roles table
CREATE TABLE IF NOT EXISTS auth.roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- User roles table (many-to-many)
CREATE TABLE IF NOT EXISTS auth.user_roles (
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES auth.roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, role_id)
);

-- Sessions table (for refresh tokens)
CREATE TABLE IF NOT EXISTS auth.sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    refresh_token_hash VARCHAR(500) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    is_revoked BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMP WITH TIME ZONE
);

-- API keys table (for programmatic access)
CREATE TABLE IF NOT EXISTS auth.api_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    key_hash VARCHAR(500) NOT NULL,
    name VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMP WITH TIME ZONE
);

-- Permissions table
CREATE TABLE IF NOT EXISTS auth.permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    resource VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Role permissions table (many-to-many)
CREATE TABLE IF NOT EXISTS auth.role_permissions (
    role_id UUID NOT NULL REFERENCES auth.roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES auth.permissions(id) ON DELETE CASCADE,
    assigned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    PRIMARY KEY (role_id, permission_id)
);

-- Create indexes for auth schema
CREATE INDEX IF NOT EXISTS idx_users_username ON auth.users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON auth.users(email);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON auth.users(is_active);
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON auth.user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON auth.user_roles(role_id);
CREATE INDEX IF NOT EXISTS idx_role_permissions_role_id ON auth.role_permissions(role_id);
CREATE INDEX IF NOT EXISTS idx_role_permissions_permission_id ON auth.role_permissions(permission_id);
CREATE INDEX IF NOT EXISTS idx_sessions_user_id ON auth.sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_sessions_expires_at ON auth.sessions(expires_at);
CREATE INDEX IF NOT EXISTS idx_sessions_is_revoked ON auth.sessions(is_revoked);
CREATE INDEX IF NOT EXISTS idx_api_keys_user_id ON auth.api_keys(user_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_is_active ON auth.api_keys(is_active);

-- ==========================================
-- Insert default roles
-- ==========================================
INSERT INTO auth.roles (name, description) VALUES
    ('admin', 'Administrator with full system access'),
    ('developer', 'Developer with code and task access'),
    ('viewer', 'Read-only access to system')
ON CONFLICT (name) DO NOTHING;

-- ==========================================
-- Completion message
-- ==========================================
DO $$
BEGIN
    RAISE NOTICE 'Database initialization completed successfully!';
    RAISE NOTICE 'Created schemas: chat, orchestration, github, cicd, auth';
    RAISE NOTICE 'Total tables created: 20';
END $$;
