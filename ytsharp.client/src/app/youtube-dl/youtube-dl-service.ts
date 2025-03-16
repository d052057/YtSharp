import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SignalRService } from './signalr.service';
import { DownloadRequest, DownloadResponse, DownloadStatus, VideoInfo } from "./youtube-dl.model";

@Injectable({
  providedIn: 'root'
})
export class YoutubeDlService {
  private apiUrl = '/api/youtubedl';
  http = inject(HttpClient);
  signalRService = inject(SignalRService);
  downloadVideo(request: DownloadRequest): Observable<DownloadResponse> {
    return this.http.post<DownloadResponse>(`${this.apiUrl}/download`, request);
  }

  getDownloadStatus(id: string): Observable<DownloadStatus> {
    return this.http.get<DownloadStatus>(`${this.apiUrl}/status/${id}`);
  }

  getVideoInfo(url: string): Observable<VideoInfo> {
    return this.http.get<VideoInfo>(`${this.apiUrl}/info`, {
      params: { url }
    });
  }
}
