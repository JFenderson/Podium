export const environment = {
  production: false,
  // Matches https profile in launchSettings.json
  apiUrl: 'https://localhost:7290/api', 
  // Matches app.MapHub<NotificationHub>("/notificationHub") in Program.cs
  hubUrl: 'https://localhost:7290/notificationHub' 
};