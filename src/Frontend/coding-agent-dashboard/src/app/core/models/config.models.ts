/**
 * Configuration Service DTOs
 */

export interface SystemConfig {
  features: FeatureFlags;
  serviceEndpoints: ServiceEndpoints;
  rateLimits: RateLimits;
  modelSettings: ModelSettings;
  githubIntegration: GitHubIntegration;
}

export interface FeatureFlags {
  useLegacyChat: boolean;
  useLegacyOrchestration: boolean;
  useLegacyML: boolean;
  enableAgenticAI: boolean;
  enableReflection: boolean;
  enablePlanning: boolean;
}

export interface ServiceEndpoints {
  gateway: string;
  chat: string;
  orchestration: string;
  mlClassifier: string;
  github: string;
  browser: string;
  cicd: string;
  dashboard: string;
  ollama: string;
}

export interface RateLimits {
  perUser: {
    requestsPerHour: number;
  };
  perIP: {
    requestsPerMinute: number;
  };
}

export interface ModelSettings {
  defaultStrategy: string;
  maxParallelSubagents: number;
  enableOllama: boolean;
  ollamaBaseUrl: string;
}

export interface GitHubIntegration {
  enabled: boolean;
  appId?: string;
  webhookSecret?: string;
}

export interface UpdateFeatureFlagsRequest {
  useLegacyChat?: boolean;
  useLegacyOrchestration?: boolean;
  useLegacyML?: boolean;
  enableAgenticAI?: boolean;
  enableReflection?: boolean;
  enablePlanning?: boolean;
}

export interface UpdateConfigRequest {
  features?: Partial<FeatureFlags>;
  serviceEndpoints?: Partial<ServiceEndpoints>;
  rateLimits?: Partial<RateLimits>;
  modelSettings?: Partial<ModelSettings>;
  githubIntegration?: Partial<GitHubIntegration>;
}

