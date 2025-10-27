/**
 * Dashboard Service DTOs
 * These interfaces match the backend Dashboard Service (BFF) response models
 */

export interface DashboardStats {
  totalConversations: number;
  totalMessages: number;
  totalTasks: number;
  completedTasks: number;
  failedTasks: number;
  runningTasks: number;
  averageTaskDuration: number; // in seconds
  lastUpdated: string; // ISO 8601 timestamp
}

export interface EnrichedTask {
  id: string;
  title: string;
  type: string; // e.g., "BugFix", "Feature", "Refactor"
  complexity: string; // e.g., "Simple", "Medium", "Complex"
  status: string; // e.g., "Running", "Completed", "Failed"
  createdAt: string; // ISO 8601 timestamp
  completedAt?: string; // ISO 8601 timestamp
  duration?: number; // in seconds
  tokenCost?: number;
  conversationId?: string;
  pullRequestNumber?: number;
}

export interface ActivityEvent {
  timestamp: string; // ISO 8601 timestamp
  type: string; // e.g., "TaskCreated", "TaskCompleted", "MessageSent"
  description: string;
  userId?: string;
  metadata?: Record<string, any>;
}

/**
 * Helper type for paginated task responses
 */
export interface PaginatedTasksResponse {
  items: EnrichedTask[];
  totalCount: number;
  page: number;
  pageSize: number;
}
