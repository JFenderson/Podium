// Development environment configuration
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5044/api',
  apiVersion: 'v1',
  tokenKey: 'podium_access_token',
  refreshTokenKey: 'podium_refresh_token',
  userKey: 'podium_user',
  tokenExpiry: 'podium_token_expiry',
  enableDebugLogging: true,
  signalRHubUrl: 'http://localhost:5044/hubs/notifications',
  // Feature flags
  features: {
    enableNotifications: true,
    enableVideoUpload: true,
    enableRealTimeUpdates: true
  },
  // Storage settings
  storageType: 'localStorage', // or 'sessionStorage'
  // Azure Blob Storage (if used for direct uploads)
  azureBlobUrl: 'https://yourstorageaccount.blob.core.windows.net'
};