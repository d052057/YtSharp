import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DownloadRequest {
  url: string;
  audioOnly: boolean;
  options: string;
}

export interface DownloadResponse {
  downloadId: string;
}

export interface DownloadStatus {
  id: string;
  url: string;
  state: string;
  progress: number;
  downloadSpeed: string;
  eta: string;
  output: string[];
  isCompleted: boolean;
  isSuccessful: boolean;
  filePath: string;
  errorMessage: string;
}

export interface VideoInfo {
  title: string;
  description: string;
  uploadDate: string;
  uploader: string;
  duration: number;
  viewCount: number;
  // Add other video metadata properties as needed
}

@Injectable({
  providedIn: 'root'
})
export class YoutubeDlService {
  private apiUrl = '/api/youtubedl';
  http = inject(HttpClient);
  constructor() { }

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
