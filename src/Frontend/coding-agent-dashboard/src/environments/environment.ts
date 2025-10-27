export const environment = {
  production: false,
  // Existing keys (kept for backward compatibility)
  apiUrl: 'http://localhost:5000/api',
  signalRUrl: 'http://localhost:5000/hubs/chat',
  // New standardized keys for chat integration
  apiBaseUrl: 'http://localhost:5000/api',
  chatHubUrl: 'http://localhost:5000/hubs/chat',
  fileBaseUrl: 'http://localhost:5000/files',
  maxUploadSize: 50 * 1024 * 1024, // 50MB
  version: '2.0.0'
};
