import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { YoutubeDlService, DownloadStatus } from './youtube-dl-service';
import { interval, Subscription, switchMap } from 'rxjs';

@Component({
  selector: 'app-youtube-dl',
  imports: [CommonModule, FormsModule],
  templateUrl: './youtube-dl.component.html',
  styleUrl: './youtube-dl.component.scss'
})
export class YoutubeDlComponent implements OnDestroy {
  youtubeDlService = inject(YoutubeDlService);
  url: string = '';
  isDownloading: boolean = false;
  options: string = '';
  audioOnly: boolean = false;
  downloadStatus: DownloadStatus | null = null;
  output: string[] = [];
  statusSubscription: Subscription | null = null;
  currentDownloadId: string | null = null;
  ngOnDestroy(): void {
    if (this.statusSubscription) {
      this.statusSubscription.unsubscribe();
    }
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
      options: this.options
    };

    this.youtubeDlService.downloadVideo(request)
      .subscribe({
      next: (response) => {
        this.currentDownloadId = response.downloadId;
        //alert("response.downloadId:" + response.downloadId);
        this.startStatusPolling();
      },
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
  startStatusPolling(): void {
    if (!this.currentDownloadId) {
      return;
    }

    // Poll status every 1 second
    this.statusSubscription = interval(1000)
      .pipe(
        switchMap(() => this.youtubeDlService.getDownloadStatus(this.currentDownloadId!))
      )
      .subscribe({
        next: (status) => {
          this.downloadStatus = status;

          // Update UI with latest output
          if (status.output && status.output.length > this.output.length) {
            this.output = [...status.output];
          }

          // Check if download is completed
          if (status.isCompleted) {
            this.isDownloading = false;
            this.statusSubscription?.unsubscribe();

            if (status.isSuccessful) {
              this.showSuccessMessage(status.filePath);
            } else {
              this.showErrorMessage('Download failed', status.errorMessage);
            }
          }
        },
        error: (error) => {
          console.error('Error polling status:', error);
          this.isDownloading = false;
          this.statusSubscription?.unsubscribe();
        }
      });
  }
  private showVideoInfoDialog(info: any): void {
    // This would be implemented with Angular Material or another dialog library
    const infoString = JSON.stringify(info, null, 2);
    alert(`Video Information:\n${infoString}`);
  }
  fetchVideoInfo(): void {
    if (!this.url) {
      return;
    }

    this.youtubeDlService.getVideoInfo(this.url).subscribe({
      next: (info) => {
        // Open a dialog or navigate to a component to display video info
        console.log('Video info:', info);
        this.showVideoInfoDialog(info);
      },
      error: (error) => {
        console.error('Error fetching video info:', error);
        this.showErrorMessage('Failed to fetch video information', error.message);
      }
    });
  }
}
