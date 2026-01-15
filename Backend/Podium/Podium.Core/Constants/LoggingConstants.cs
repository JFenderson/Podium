namespace Podium.Core.Constants;

/// <summary>
/// Defines event IDs for structured logging across the application.
/// Use these event IDs to categorize and filter log entries.
/// </summary>
public static class LoggingConstants
{
    /// <summary>
    /// Authentication-related event IDs (1000-1099)
    /// <example>
    /// _logger.LogInformation(LoggingConstants.Authentication.LoginSuccess, 
    ///     "User {UserId} logged in successfully from {IpAddress}", userId, ipAddress);
    /// </example>
    /// </summary>
    public static class Authentication
    {
        public const int LoginAttempt = 1000;
        public const int LoginSuccess = 1001;
        public const int LoginFailure = 1002;
        public const int RegistrationAttempt = 1010;
        public const int RegistrationSuccess = 1011;
        public const int RegistrationFailure = 1012;
        public const int EmailConfirmation = 1020;
        public const int PasswordReset = 1021;
        public const int TokenRefresh = 1030;
        public const int Logout = 1040;
        public const int UnauthorizedAccess = 1050;
    }

    /// <summary>
    /// Video management event IDs (2000-2099)
    /// <example>
    /// _logger.LogInformation(LoggingConstants.VideoManagement.UploadStarted, 
    ///     "Video upload started for user {UserId}, size {FileSizeBytes} bytes", userId, fileSize);
    /// </example>
    /// </summary>
    public static class VideoManagement
    {
        public const int UploadStarted = 2000;
        public const int UploadCompleted = 2001;
        public const int UploadFailed = 2002;
        public const int TranscodingStarted = 2010;
        public const int TranscodingCompleted = 2011;
        public const int TranscodingFailed = 2012;
        public const int VideoDeleted = 2020;
        public const int VideoRetrieved = 2021;
        public const int VideoUpdated = 2022;
        public const int StorageError = 2030;
    }

    /// <summary>
    /// Scholarship offer event IDs (3000-3099)
    /// <example>
    /// _logger.LogInformation(LoggingConstants.ScholarshipOffers.OfferCreated, 
    ///     "Scholarship offer {OfferId} created for student {StudentId} by {BandStaffId}", 
    ///     offerId, studentId, bandStaffId);
    /// </example>
    /// </summary>
    public static class ScholarshipOffers
    {
        public const int OfferCreated = 3000;
        public const int OfferAccepted = 3001;
        public const int OfferRejected = 3002;
        public const int OfferExpired = 3010;
        public const int OfferWithdrawn = 3011;
        public const int OfferApprovalRequested = 3020;
        public const int OfferApproved = 3021;
        public const int OfferRejectedByDirector = 3022;
    }

    /// <summary>
    /// Database operation event IDs (4000-4099)
    /// <example>
    /// _logger.LogWarning(LoggingConstants.Database.SlowQuery, 
    ///     "Slow query detected: {QueryName} took {DurationMs}ms", queryName, duration);
    /// </example>
    /// </summary>
    public static class Database
    {
        public const int QueryExecuted = 4000;
        public const int SlowQuery = 4001;
        public const int QueryFailed = 4002;
        public const int ConnectionError = 4010;
        public const int MigrationStarted = 4020;
        public const int MigrationCompleted = 4021;
        public const int MigrationFailed = 4022;
        public const int DeadlockDetected = 4030;
    }

    /// <summary>
    /// External API event IDs (5000-5099)
    /// <example>
    /// _logger.LogInformation(LoggingConstants.ExternalApis.ApiCallSuccess, 
    ///     "External API call successful: {ApiName} in {DurationMs}ms", apiName, duration);
    /// </example>
    /// </summary>
    public static class ExternalApis
    {
        public const int ApiCallStarted = 5000;
        public const int ApiCallSuccess = 5001;
        public const int ApiCallFailed = 5002;
        public const int ApiTimeout = 5010;
        public const int ApiRateLimited = 5011;
        public const int SendGridEmail = 5020;
        public const int AzureStorageOperation = 5030;
        public const int AwsS3Operation = 5031;
    }

    /// <summary>
    /// Background job event IDs (6000-6099)
    /// </summary>
    public static class BackgroundJobs
    {
        public const int JobStarted = 6000;
        public const int JobCompleted = 6001;
        public const int JobFailed = 6002;
        public const int HangfireError = 6010;
    }

    /// <summary>
    /// Performance monitoring event IDs (7000-7099)
    /// </summary>
    public static class Performance
    {
        public const int SlowRequest = 7000;
        public const int HighMemoryUsage = 7001;
        public const int HighCpuUsage = 7002;
    }
}
