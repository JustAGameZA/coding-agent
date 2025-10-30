-- 003-migrate-tasks.sql
-- Migrate legacy tasks into orchestration schema

INSERT INTO orchestration.coding_tasks (id, conversation_id, task_type, complexity, title, description, status, priority, created_at, updated_at, started_at, completed_at, metadata)
SELECT t.id, t.conversation_id, t.task_type, t.complexity, t.title, t.description, COALESCE(t.status, 'pending'), COALESCE(t.priority, 0), COALESCE(t.created_at, NOW()), COALESCE(t.updated_at, NOW()), t.started_at, t.completed_at, t.metadata
FROM legacy.tasks t
LEFT JOIN orchestration.coding_tasks ct ON ct.id = t.id
WHERE ct.id IS NULL;

INSERT INTO orchestration.task_executions (id, task_id, execution_strategy, status, started_at, completed_at, error_message, metadata)
SELECT e.id, e.task_id, e.execution_strategy, COALESCE(e.status, 'pending'), COALESCE(e.started_at, NOW()), e.completed_at, e.error_message, e.metadata
FROM legacy.task_executions e
LEFT JOIN orchestration.task_executions te ON te.id = e.id
WHERE te.id IS NULL;
