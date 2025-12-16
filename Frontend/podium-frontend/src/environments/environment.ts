export const environment = {
  production: true,
  apiUrl: 'https://api.podium.com/api', // Update with your production API URL
  apiVersion: 'v1',
  tokenKey: 'podium_access_token',
  refreshTokenKey: 'podium_refresh_token',
  userKey: 'podium_user',
  tokenExpiry: 'podium_token_expiry',
  enableDebugLogging: false,
  signalRHubUrl: 'https://api.podium.com/hubs/notifications',
  // Feature flags
  features: {
    enableNotifications: true,
    enableVideoUpload: true,
    enableRealTimeUpdates: true
  },
  // Storage settings
  storageType: 'localStorage',
  // Azure Blob Storage
  azureBlobUrl: 'https://yourstorageaccount.blob.core.windows.net'
};