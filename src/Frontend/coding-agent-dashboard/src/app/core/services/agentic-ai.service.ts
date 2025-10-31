import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * Service for interacting with Agentic AI features
 */
@Injectable({ providedIn: 'root' })
export class AgenticAiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}`;

  /**
   * Get reflection results for an execution
   */
  getReflection(executionId: string): Observable<ReflectionResult> {
    return this.http.get<ReflectionResult>(`${this.apiUrl}/orchestration/executions/${executionId}/reflection`);
  }

  /**
   * Get plan details for a task
   */
  getPlan(taskId: string): Observable<Plan> {
    return this.http.get<Plan>(`${this.apiUrl}/orchestration/tasks/${taskId}/plan`);
  }

  /**
   * Get plan steps
   */
  getPlanSteps(planId: string): Observable<PlanStep[]> {
    return this.http.get<PlanStep[]>(`${this.apiUrl}/orchestration/plans/${planId}/steps`);
  }

  /**
   * Get next step in plan
   */
  getNextPlanStep(planId: string): Observable<PlanStep> {
    return this.http.get<PlanStep>(`${this.apiUrl}/orchestration/plans/${planId}/next-step`);
  }

  /**
   * Submit feedback for a task
   */
  submitFeedback(feedback: FeedbackRequest): Observable<Feedback> {
    return this.http.post<Feedback>(`${this.apiUrl}/orchestration/feedback`, feedback);
  }

  /**
   * Get feedback analysis for a task
   */
  getFeedbackAnalysis(taskId: string): Observable<FeedbackAnalysis> {
    return this.http.get<FeedbackAnalysis>(`${this.apiUrl}/orchestration/feedback/task/${taskId}/analysis`);
  }

  /**
   * Get memory context for a query
   */
  getMemoryContext(query: string, episodeLimit = 5, semanticLimit = 10): Observable<MemoryContext> {
    return this.http.get<MemoryContext>(`${this.apiUrl}/memory/context`, {
      params: { query, episodeLimit: episodeLimit.toString(), semanticLimit: semanticLimit.toString() }
    });
  }

  /**
   * Get thinking process for a goal
   */
  getThinkingProcess(processId: string): Observable<ThinkingProcess> {
    return this.http.get<ThinkingProcess>(`${this.apiUrl}/orchestration/thinking/${processId}`);
  }

  /**
   * Get thoughts for a thinking process
   */
  getThoughts(processId: string): Observable<Thought[]> {
    return this.http.get<Thought[]>(`${this.apiUrl}/orchestration/thinking/${processId}/thoughts`);
  }
}

// Data models
export interface ReflectionResult {
  executionId: string;
  strengths: string[];
  weaknesses: string[];
  keyLessons: string[];
  improvementSuggestions: string[];
  confidenceScore: number;
  contextPattern: Record<string, any>;
}

export interface Plan {
  id: string;
  goal: string;
  description: string;
  subTasks: PlanStep[];
  estimatedTotalEffort: string;
  risks: string[];
  status: 'Created' | 'InProgress' | 'Completed' | 'Failed' | 'Cancelled';
  createdAt: string;
}

export interface PlanStep {
  id: string;
  description: string;
  dependencies: string[];
  estimatedEffort: 'low' | 'medium' | 'high';
  validationCriteria: string;
  subSteps: PlanStep[];
  status: 'Pending' | 'InProgress' | 'Completed' | 'Failed' | 'Skipped';
  result?: ExecutionResult;
}

export interface ExecutionResult {
  success: boolean;
  error?: string;
  results: Record<string, any>;
  duration: string;
}

export interface FeedbackRequest {
  taskId: string;
  executionId?: string;
  userId: string;
  type: 'Positive' | 'Negative' | 'Neutral';
  rating: number; // 0.0 to 1.0
  reason?: string;
  context?: Record<string, any>;
  procedureId?: string;
}

export interface Feedback {
  id: string;
  taskId: string;
  executionId?: string;
  userId: string;
  type: 'Positive' | 'Negative' | 'Neutral';
  rating: number;
  reason?: string;
  context: Record<string, any>;
  procedureId?: string;
  createdAt: string;
}

export interface FeedbackAnalysis {
  taskId: string;
  patterns: FeedbackPattern[];
  recommendations: string[];
  hasSignificantChanges: boolean;
}

export interface FeedbackPattern {
  procedureId: string;
  newSuccessRate: number;
  improvedSteps: any[];
  patternDescription: string;
}

export interface MemoryContext {
  episodicKnowledge: Episode[];
  semanticKnowledge: SemanticMemory[];
  relevantProcedures: Procedure[];
  relevanceScores: Record<string, number>;
}

export interface Episode {
  id: string;
  taskId?: string;
  executionId?: string;
  userId: string;
  timestamp: string;
  eventType: string;
  context: Record<string, any>;
  outcome: Record<string, any>;
  learnedPatterns: string[];
  createdAt: string;
}

export interface SemanticMemory {
  id: string;
  contentType: string;
  content: string;
  metadata: Record<string, any>;
  sourceEpisodeId?: string;
  confidenceScore: number;
  createdAt: string;
}

export interface Procedure {
  id: string;
  procedureName: string;
  description: string;
  contextPattern: Record<string, any>;
  steps: ProcedureStep[];
  successRate: number;
  usageCount: number;
  lastUsedAt?: string;
}

export interface ProcedureStep {
  order: number;
  description: string;
  parameters: Record<string, any>;
  validationCriteria?: string;
}

export interface ThinkingProcess {
  id: string;
  goal: string;
  startTime: string;
  endTime?: string;
  thoughts: Thought[];
  strategyAdjustments: ThinkingStrategy[];
}

export interface Thought {
  timestamp: string;
  content: string;
  type: 'Observation' | 'Hypothesis' | 'Decision' | 'Reflection';
  confidence: number;
}

export interface ThinkingStrategy {
  name: string;
  description: string;
  parameters: Record<string, any>;
  appliedAt: string;
}

