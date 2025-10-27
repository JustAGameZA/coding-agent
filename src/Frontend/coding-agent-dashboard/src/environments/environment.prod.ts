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
  dashboardServiceUrl: 'http://localhost:5007',
  version: '2.0.0'
};
