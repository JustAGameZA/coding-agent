// Network environment - for accessing from other devices on your local network
// Replace YOUR_HOST_IP with your actual IP address (e.g., 192.168.1.100)
export const environment = {
  production: false,
  // Use your machine's network IP instead of localhost
  apiUrl: 'http://YOUR_HOST_IP:5000/api',
  signalRUrl: 'http://YOUR_HOST_IP:5000/hubs/chat',
  apiBaseUrl: 'http://YOUR_HOST_IP:5000/api',
  chatHubUrl: 'http://YOUR_HOST_IP:5000/hubs/chat',
  fileBaseUrl: 'http://YOUR_HOST_IP:5000/files',
  maxUploadSize: 50 * 1024 * 1024, // 50MB
  // Dashboard Service (BFF)
  dashboardServiceUrl: 'http://YOUR_HOST_IP:5007',
  version: '2.0.0-network'
};
