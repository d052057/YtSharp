import { CommonModule, JsonPipe } from '@angular/common';
import { Component, inject, ApplicationRef, OnInit, signal, Signal, ChangeDetectorRef, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { YoutubeDlService } from './youtube-dl-service';
import { SignalRService } from './signalr.service';
import { interval, Subscription, switchMap, BehaviorSubject } from 'rxjs';
import { DownloadStatus } from "./youtube-dl.model";
import { throttleTime, auditTime, debounceTime } from 'rxjs/operators';
import { v4 as uuidv4 } from 'uuid';

@Component({
  selector: 'app-youtube-dl',
  imports: [CommonModule, FormsModule],
  templateUrl: './youtube-dl.component.html',
  styleUrl: './youtube-dl.component.scss'
})
export class YoutubeDlComponent {
  youtubeDlService = inject(YoutubeDlService);
  signalRService = inject(SignalRService);
  cdRef = inject(ChangeDetectorRef);
  appRef = inject(ApplicationRef);
  ngZone = inject(NgZone);
  url: string = '';
  isDownloading: boolean = false;
  options: string = '';
  audioOnly: boolean = false;
  public downloadStatus = new BehaviorSubject<DownloadStatus | null>(null);
  output: string[] = [];
  currentDownloadId: string;
  private lastStatus: string | null = null;
  constructor() {
    this.currentDownloadId = uuidv4();
    // Start SignalR connection
    this.signalRService.startConnection();

    // Subscribe to progress updates from SignalR
    this.signalRService.progress$
      .pipe(debounceTime(500))
      /*.pipe(throttleTime(1000)) // Adjust the time interval as needed*/
      /*.pipe(auditTime(1000)) // Processes only the latest update every second*/
      .subscribe(({ downloadId, status }) => {

        if (downloadId === this.currentDownloadId) {
          const newStatus = JSON.stringify(status);
          if (newStatus !== this.lastStatus) {
            this.lastStatus = newStatus;
            this.downloadStatus.next({ ...status });

            // Update UI with latest output
            if (status.output && status.output.length > this.output.length) {
              this.output = [...status.output];
            }

            // Check if download is completed
            if (status.isCompleted) {
              this.isDownloading = false;
              this.cdRef.detectChanges();
              if (status.isSuccessful) {
                this.showSuccessMessage(status.filePath);
              } else {
                this.showErrorMessage('Download failed', status.errorMessage);
              }
            }
            this.appRef.tick(); // Force full UI re-check
          }
        }
      });
  }

  startDownload(): void {
    if (!this.url || this.isDownloading) {
      return;
    }

    this.isDownloading = true;
    this.output = [];

    const request = {
      url: this.url,
      audioOnly: this.audioOnly,
      options: this.options,
      downloadId: this.currentDownloadId
    };

    this.youtubeDlService.downloadVideo(request)
      .subscribe({
        //next: (response) => {
        //  this.currentDownloadId = response.downloadId;
        //},
        error: (error) => {
          console.error('Error starting download:', error);
          this.isDownloading = false;
          this.showErrorMessage('Failed to start download', error.message);
        }
      });

  }
  private showErrorMessage(title: string, message: string): void {
    alert(`${title}: ${message}`);
  }
  private showSuccessMessage(filePath: string): void {
    alert(`Successfully downloaded "${this.url}" to:\n"${filePath}".`);
  }
  fetchVideoInfo(): void {
    if (!this.url) {
      return;
    }

    this.youtubeDlService.getVideoInfo(this.url).subscribe({
      next: (info) => {
        // Open a dialog or navigate to a component to display video info
        /*       console.log('Video info:', info);*/
        this.showVideoInfoDialog(info);
      },
      error: (error) => {
        console.error('Error fetching video info:', error);
        this.showErrorMessage('Failed to fetch video information', error.message);
      },
    });
  }
  private showVideoInfoDialog(info: any): void {
    // This would be implemented with Angular Material or another dialog library
    const infoString = JSON.stringify(info, null, 2);
    alert(`Video Information:\n${infoString}`);
  }
}
