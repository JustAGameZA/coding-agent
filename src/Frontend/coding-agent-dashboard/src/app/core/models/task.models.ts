/**
 * Task Service DTOs
 * These interfaces match the backend Orchestration Service API
 */

export interface CreateTaskRequest {
  title: string;
  description: string;
}

export interface UpdateTaskRequest {
  title: string;
  description: string;
}

export type ExecutionStrategy = 'SingleShot' | 'Iterative' | 'MultiAgent' | 'HybridExecution';

export interface ExecuteTaskRequest {
  strategy?: ExecutionStrategy;
  maxParallelSubagents?: number;
}

export interface ExecuteTaskResponse {
  taskId: string;
  executionId: string;
  strategy: ExecutionStrategy;
  message: string;
}

export interface TaskDto {
  id: string;
  title: string;
  description: string;
  type: string;
  complexity: string;
  status: TaskStatus;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  duration?: number;
  tokenCost?: number;
  pullRequestNumber?: number;
}

export interface TaskDetailDto extends TaskDto {
  executions: ExecutionDto[];
  context?: string;
}

export interface ExecutionDto {
  id: string;
  taskId: string;
  strategy: ExecutionStrategy;
  modelUsed: string;
  status: ExecutionStatus;
  errorMessage?: string;
  startedAt: string;
  completedAt?: string;
  tokensUsed: number;
  costUSD: number;
}

export type TaskStatus = 'Pending' | 'Classifying' | 'InProgress' | 'Completed' | 'Failed' | 'Cancelled';
export type ExecutionStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

