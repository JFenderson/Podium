// Notification DTOs matching backend

export interface NotificationDto {
  notificationId: number;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  relatedEntityId?: number;
  relatedEntityType?: string;
  actionUrl?: string;
  isRead: boolean;
  isPinned: boolean;
  priority: NotificationPriority;
  createdAt: Date;
  readAt?: Date;
  expiresAt?: Date;
  
}

export enum NotificationType {
  OfferReceived = 'OfferReceived',
  OfferAccepted = 'OfferAccepted',
  OfferDeclined = 'OfferDeclined',
  OfferExpired = 'OfferExpired',
  OfferWithdrawn = 'OfferWithdrawn',
  ContactRequest = 'ContactRequest',
  ContactApproved = 'ContactApproved',
  ContactDenied = 'ContactDenied',
  RatingReceived = 'RatingReceived',
  GuardianApprovalNeeded = 'GuardianApprovalNeeded',
  GuardianApprovalGranted = 'GuardianApprovalGranted',
  GuardianApprovalDenied = 'GuardianApprovalDenied',
  VideoUploaded = 'VideoUploaded',
  VideoProcessed = 'VideoProcessed',
  VideoFailed = 'VideoFailed',
  EventCreated = 'EventCreated',
  EventReminder = 'EventReminder',
  EventCancelled = 'EventCancelled',
  InterestReceived = 'InterestReceived',
  ProfileViewed = 'ProfileViewed',
  System = 'System',
  Announcement = 'Announcement'
}

export enum NotificationPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Urgent = 'Urgent'
}

export interface CreateNotificationDto {
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  relatedEntityId?: number;
  relatedEntityType?: string;
  actionUrl?: string;
  priority?: NotificationPriority;
  expiresAt?: Date;
}

export interface NotificationPreferencesDto {
  userId: string;
  emailNotifications: boolean;
  pushNotifications: boolean;
  smsNotifications: boolean;
  offerNotifications: boolean;
  contactNotifications: boolean;
  ratingNotifications: boolean;
  eventNotifications: boolean;
  systemNotifications: boolean;
  notificationFrequency: NotificationFrequency;
}

export enum NotificationFrequency {
  Immediate = 'Immediate',
  Hourly = 'Hourly',
  Daily = 'Daily',
  Weekly = 'Weekly'
}

export interface UpdateNotificationPreferencesDto {
  emailNotifications?: boolean;
  pushNotifications?: boolean;
  smsNotifications?: boolean;
  offerNotifications?: boolean;
  contactNotifications?: boolean;
  ratingNotifications?: boolean;
  eventNotifications?: boolean;
  systemNotifications?: boolean;
  notificationFrequency?: NotificationFrequency;
}

export interface NotificationSummaryDto {
  totalCount: number;
  unreadCount: number;
  byType: NotificationTypeCount[];
  byPriority: NotificationPriorityCount[];
}

export interface NotificationTypeCount {
  type: NotificationType;
  count: number;
}

export interface NotificationPriorityCount {
  priority: NotificationPriority;
  count: number;
}

export interface MarkNotificationsReadDto {
  notificationIds: number[];
}

export interface NotificationFilterDto {
  type?: NotificationType;
  priority?: NotificationPriority;
  isRead?: boolean;
  since?: Date;
  pageNumber?: number;
  pageSize?: number;
}