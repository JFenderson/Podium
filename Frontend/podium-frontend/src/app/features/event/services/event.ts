import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api';
import {
  EventDto,
  CreateEventDto,
  UpdateEventDto,
  EventRegistrationDto,
  RegisterForEventDto,
  EventSummaryDto,
  EventFilterDto,
  UpcomingEventsDto
} from '../../../core/models/event';
import { PagedResult } from '../../../core/models/student';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private readonly endpoint = 'Events';

  constructor(private api: ApiService) {}

  /**
   * Get all events with filtering
   */
  getEvents(filter?: EventFilterDto): Observable<PagedResult<EventDto>> {
    return this.api.get<PagedResult<EventDto>>(this.endpoint, filter);
  }

  /**
   * Get event by ID
   */
  getEvent(id: number): Observable<EventDto> {
    return this.api.get<EventDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Create new event (BandStaff with permission)
   */
  createEvent(dto: CreateEventDto): Observable<EventDto> {
    return this.api.post<EventDto>(this.endpoint, dto);
  }

  /**
   * Update event (BandStaff with permission)
   */
  updateEvent(id: number, dto: UpdateEventDto): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}`, dto);
  }

  /**
   * Delete event (BandStaff with permission)
   */
  deleteEvent(id: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get upcoming events
   */
  getUpcomingEvents(bandId?: number, limit: number = 10): Observable<UpcomingEventsDto> {
    return this.api.get<UpcomingEventsDto>(`${this.endpoint}/upcoming`, {
      bandId,
      limit
    });
  }

  /**
   * Get public events
   */
  getPublicEvents(filter?: EventFilterDto): Observable<PagedResult<EventDto>> {
    return this.api.get<PagedResult<EventDto>>(`${this.endpoint}/public`, filter);
  }

  /**
   * Get events by band
   */
  getEventsByBand(bandId: number): Observable<EventDto[]> {
    return this.api.get<EventDto[]>(`${this.endpoint}/band/${bandId}`);
  }

  /**
   * Register for event (Students)
   */
  registerForEvent(dto: RegisterForEventDto): Observable<EventRegistrationDto> {
    return this.api.post<EventRegistrationDto>(`${this.endpoint}/register`, dto);
  }

  /**
   * Cancel event registration
   */
  cancelRegistration(eventId: number, studentId: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${eventId}/registration/${studentId}`);
  }

  /**
   * Get event registrations (BandStaff)
   */
  getEventRegistrations(eventId: number): Observable<EventRegistrationDto[]> {
    return this.api.get<EventRegistrationDto[]>(`${this.endpoint}/${eventId}/registrations`);
  }

  /**
   * Mark attendee as attended
   */
  markAttended(eventId: number, studentId: number, attended: boolean): Observable<any> {
    return this.api.post(`${this.endpoint}/${eventId}/attendance`, {
      studentId,
      attended
    });
  }

  /**
   * Get my registered events (Student)
   */
  getMyRegisteredEvents(): Observable<EventDto[]> {
    return this.api.get<EventDto[]>(`${this.endpoint}/my-registrations`);
  }

  /**
   * Search events
   */
  searchEvents(searchTerm: string, isPublic?: boolean): Observable<EventSummaryDto[]> {
    return this.api.get<EventSummaryDto[]>(`${this.endpoint}/search`, {
      search: searchTerm,
      isPublic
    });
  }

  /**
   * Get event attendance statistics
   */
  getEventStats(eventId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/${eventId}/stats`);
  }

  /**
   * Send event reminders
   */
  sendEventReminders(eventId: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${eventId}/send-reminders`, {});
  }

  /**
   * Cancel event
   */
  cancelEvent(eventId: number, reason?: string): Observable<any> {
    return this.api.post(`${this.endpoint}/${eventId}/cancel`, { reason });
  }

  /**
   * Get events calendar data
   */
  getEventsCalendar(bandId?: number, month?: number, year?: number): Observable<any> {
    return this.api.get(`${this.endpoint}/calendar`, {
      bandId,
      month,
      year
    });
  }

  /**
   * Export event registrations
   */
  exportRegistrations(eventId: number, format: 'csv' | 'excel'): Observable<Blob> {
    return this.api.download(`${this.endpoint}/${eventId}/export`, { format });
  }
}