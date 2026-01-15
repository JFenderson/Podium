import { HttpParams } from "@angular/common/http";
import { HttpClient } from "@microsoft/signalr";
import { Observable } from "rxjs";
import { PagedResult } from "../../../core/models/common.models";
import { VideoThumbnail } from "../../../core/models/video.models";
import { Injectable } from "@angular/core";
import { ApiService } from "../../../core/services/api.service";

@Injectable({
  providedIn: 'root',
})
export class VideoService {
  private readonly endpoint = 'Videos';

  constructor(private api: ApiService, private http: HttpClient) {}


  getStudentThumbnails(studentId: number, page: number = 1, pageSize: number = 12): Observable<PagedResult<VideoThumbnail>> {
      const params = new HttpParams()
        .set('page', page.toString())
        .set('pageSize', pageSize.toString());
        
      return this.api.get<PagedResult<VideoThumbnail>>(`${this.endpoint}/student/${studentId}/thumbnails`, { params });
    }
}

