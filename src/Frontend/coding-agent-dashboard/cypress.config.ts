import { defineConfig } from 'cypress';

export default defineConfig({
  e2e: {
    baseUrl: 'http://localhost:4200',
    supportFile: 'cypress/support/e2e.ts',
    specPattern: 'cypress/e2e/**/*.cy.ts',
    videosFolder: 'cypress/videos',
    screenshotsFolder: 'cypress/screenshots',
    fixturesFolder: 'cypress/fixtures',
    
    // Viewport
    viewportWidth: 1280,
    viewportHeight: 720,
    
    // Timeouts
    defaultCommandTimeout: 10000,
    requestTimeout: 10000,
    responseTimeout: 30000,
    
    // Retries
    retries: {
      runMode: 2,
      openMode: 0
    },
    
    // Video/Screenshot settings
    video: true,
    screenshotOnRunFailure: true,
    
    // Environment variables
    env: {
      apiBaseUrl: 'http://localhost:5000',
      dashboardServiceUrl: 'http://localhost:5003',
      chatServiceUrl: 'http://localhost:5001',
      orchestrationServiceUrl: 'http://localhost:5002',
      signalRHubUrl: 'http://localhost:5001/hubs/chat'
    },
    
    setupNodeEvents(on, config) {
      // Implement node event listeners here if needed
      return config;
    },
  },
});
