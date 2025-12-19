// Event DTOs matching backend

export interface EventDto {
  eventId: number;
  bandId: number;
  bandName?: string;
  title: string;
  description?: string;
  eventType: EventType;
  location?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  startDate: Date;
  endDate?: Date;
  registrationDeadline?: Date;
  maxAttendees?: number;
  currentAttendees: number;
  isPublic: boolean;
  requiresRsvp: boolean;
  contactEmail?: string;
  contactPhone?: string;
  additionalInfo?: string;
  createdByUserId: string;
  createdByName?: string;
  createdAt: Date;
  updatedAt?: Date;
}

export interface CreateEventDto {
  bandId: number;
  title: string;
  description?: string;
  eventType: EventType;
  location?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  startDate: Date;
  endDate?: Date;
  registrationDeadline?: Date;
  maxAttendees?: number;
  isPublic: boolean;
  requiresRsvp: boolean;
  contactEmail?: string;
  contactPhone?: string;
  additionalInfo?: string;
}

export interface UpdateEventDto {
  title?: string;
  description?: string;
  eventType?: EventType;
  location?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  startDate?: Date;
  endDate?: Date;
  registrationDeadline?: Date;
  maxAttendees?: number;
  isPublic?: boolean;
  requiresRsvp?: boolean;
  contactEmail?: string;
  contactPhone?: string;
  additionalInfo?: string;
}

export enum EventType {
  Audition = 'Audition',
  Clinic = 'Clinic',
  Showcase = 'Showcase',
  OpenHouse = 'OpenHouse',
  BandCamp = 'BandCamp',
  RecruitmentVisit = 'RecruitmentVisit',
  Performance = 'Performance',
  SocialEvent = 'SocialEvent',
  Information = 'Information',
  Other = 'Other'
}

export interface EventRegistrationDto {
  registrationId: number;
  eventId: number;
  studentId: number;
  studentName?: string;
  registeredAt: Date;
  attended: boolean;
  notes?: string;
}

export interface RegisterForEventDto {
  eventId: number;
  studentId: number;
  notes?: string;
}

export interface EventSummaryDto {
  eventId: number;
  title: string;
  eventType: EventType;
  startDate: Date;
  location?: string;
  city?: string;
  state?: string;
  currentAttendees: number;
  maxAttendees?: number;
  isPublic: boolean;
  requiresRsvp: boolean;
}

export interface EventFilterDto {
  bandId?: number;
  eventType?: EventType;
  state?: string;
  city?: string;
  startDateFrom?: Date;
  startDateTo?: Date;
  isPublic?: boolean;
  requiresRsvp?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface UpcomingEventsDto {
  events: EventSummaryDto[];
  totalCount: number;
}