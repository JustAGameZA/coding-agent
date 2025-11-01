// Note: These types should match the actual models in the app
// For E2E tests, we define minimal interfaces to avoid circular dependencies
export type ExecutionStrategy = 'SingleShot' | 'Iterative' | 'MultiAgent' | 'HybridExecution';
export type TaskStatus = 'Pending' | 'Classifying' | 'InProgress' | 'Completed' | 'Failed' | 'Cancelled';
export type ExecutionStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

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

/**
 * Task Factory
 * Creates mock task data for E2E tests
 */

export class TaskFactory {
  /**
   * Create a basic task
   */
  static createTask(overrides: Partial<TaskDto> = {}): TaskDto {
    return {
      id: overrides.id || `task-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      title: overrides.title || 'Test Task',
      description: overrides.description || 'This is a test task description',
      type: overrides.type || 'Feature',
      complexity: overrides.complexity || 'Medium',
      status: overrides.status || 'Pending',
      createdAt: overrides.createdAt || new Date().toISOString(),
      startedAt: overrides.startedAt,
      completedAt: overrides.completedAt,
      duration: overrides.duration,
      tokenCost: overrides.tokenCost,
      pullRequestNumber: overrides.pullRequestNumber
    };
  }
  
  /**
   * Create a pending task
   */
  static createPendingTask(overrides: Partial<TaskDto> = {}): TaskDto {
    return this.createTask({
      status: 'Pending',
      ...overrides
    });
  }
  
  /**
   * Create a running task
   */
  static createRunningTask(overrides: Partial<TaskDto> = {}): TaskDto {
    return this.createTask({
      status: 'InProgress',
      startedAt: overrides.startedAt || new Date().toISOString(),
      ...overrides
    });
  }
  
  /**
   * Create a completed task
   */
  static createCompletedTask(overrides: Partial<TaskDto> = {}): TaskDto {
    const startedAt = overrides.startedAt || new Date(Date.now() - 3600000).toISOString();
    const completedAt = overrides.completedAt || new Date().toISOString();
    
    return this.createTask({
      status: 'Completed',
      startedAt,
      completedAt,
      duration: overrides.duration || 3600,
      tokenCost: overrides.tokenCost || 0.12,
      ...overrides
    });
  }
  
  /**
   * Create a failed task
   */
  static createFailedTask(overrides: Partial<TaskDto> = {}): TaskDto {
    const startedAt = overrides.startedAt || new Date(Date.now() - 1800000).toISOString();
    const completedAt = overrides.completedAt || new Date().toISOString();
    
    return this.createTask({
      status: 'Failed',
      startedAt,
      completedAt,
      duration: overrides.duration || 1800,
      ...overrides
    });
  }
  
  /**
   * Create a task detail with executions
   */
  static createTaskDetail(overrides: Partial<TaskDetailDto> = {}): TaskDetailDto {
    const task = this.createTask(overrides);
    
    return {
      ...task,
      executions: overrides.executions || [],
      context: overrides.context || 'Default context'
    };
  }
  
  /**
   * Create an execution
   */
  static createExecution(overrides: Partial<ExecutionDto> = {}): ExecutionDto {
    const taskId = overrides.taskId || `task-${Date.now()}`;
    const startedAt = overrides.startedAt || new Date().toISOString();
    
    return {
      id: overrides.id || `exec-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      taskId,
      strategy: overrides.strategy || 'Iterative',
      modelUsed: overrides.modelUsed || 'mistral:7b',
      status: overrides.status || 'Running',
      errorMessage: overrides.errorMessage,
      startedAt,
      completedAt: overrides.completedAt,
      tokensUsed: overrides.tokensUsed || 1000,
      costUSD: overrides.costUSD || 0.01
    };
  }
  
  /**
   * Create a completed execution
   */
  static createCompletedExecution(overrides: Partial<ExecutionDto> = {}): ExecutionDto {
    const startedAt = overrides.startedAt || new Date(Date.now() - 3600000).toISOString();
    const completedAt = overrides.completedAt || new Date().toISOString();
    
    return this.createExecution({
      status: 'Completed',
      startedAt,
      completedAt,
      tokensUsed: overrides.tokensUsed || 5000,
      costUSD: overrides.costUSD || 0.12,
      ...overrides
    });
  }
  
  /**
   * Create a failed execution
   */
  static createFailedExecution(overrides: Partial<ExecutionDto> = {}): ExecutionDto {
    const startedAt = overrides.startedAt || new Date(Date.now() - 1800000).toISOString();
    const completedAt = overrides.completedAt || new Date().toISOString();
    
    return this.createExecution({
      status: 'Failed',
      errorMessage: overrides.errorMessage || 'Execution failed',
      startedAt,
      completedAt,
      tokensUsed: overrides.tokensUsed || 2000,
      costUSD: overrides.costUSD || 0.04,
      ...overrides
    });
  }
  
  /**
   * Create multiple tasks
   */
  static createTasks(count: number, status?: TaskStatus): TaskDto[] {
    const tasks: TaskDto[] = [];
    for (let i = 0; i < count; i++) {
      const task = status 
        ? this.createTask({ status })
        : this.createTask();
      task.title = `Task ${i + 1}`;
      tasks.push(task);
    }
    return tasks;
  }
  
  /**
   * Create task with execution
   */
  static createTaskWithExecution(
    taskOverrides: Partial<TaskDetailDto> = {},
    executionOverrides: Partial<ExecutionDto> = {}
  ): TaskDetailDto {
    const task = this.createTask(taskOverrides);
    const execution = this.createExecution({
      taskId: task.id,
      ...executionOverrides
    });
    
    return {
      ...task,
      executions: [execution],
      context: taskOverrides.context || 'Default context'
    };
  }
}

