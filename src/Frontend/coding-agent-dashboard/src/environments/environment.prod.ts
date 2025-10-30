export const environment = {
  production: true,
  // Existing keys (kept for backward compatibility)
  apiUrl: '/api',
  signalRUrl: '/hubs/chat',
  // New standardized keys for chat integration
  apiBaseUrl: '/api',
  chatHubUrl: '/hubs/chat',
  fileBaseUrl: '/files',
  maxUploadSize: 50 * 1024 * 1024, // 50MB
  // Dashboard Service (BFF)
  // Use relative path for production to go through Gateway routing
  dashboardServiceUrl: '/api/dashboard',
  version: '2.0.0'
};
