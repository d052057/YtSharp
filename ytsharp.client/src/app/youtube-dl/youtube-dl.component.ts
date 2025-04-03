import { CommonModule, JsonPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SignalrServiceService } from './signalr-service.service';
import { DownloadStatus, DownloadRequest } from "./youtube-dl.model";


@Component({
  selector: 'app-youtube-dl',
  imports: [CommonModule, FormsModule],
  templateUrl: './youtube-dl.component.html',
  styleUrl: './youtube-dl.component.scss'
})
export class YoutubeDlComponent implements OnInit  {
  signalRService = inject(SignalrServiceService);
  url: string = 'https://www.youtube.com/watch?v=DSH-RzULrNs&list=RDDSH-RzULrNs&start_radio=1';
  isDownloading: boolean = false;
  options: string = '';
  audioOnly: boolean = false;
  output: string[] = [];
  progress: string = '';
  error: string = '';
  ReceiveSpeed: string = '';
  ReceiveETA: string = '';
  ReceiveData: string = '';
  ReceiveVideoIndex: string = '';  
  ReceiveTotalSize: string = '';  
  finish: string = '';
  ReceiveState: string = '';  
  outputFolder: string = 'c:\\medias\\poster';
  ngOnInit(): void {
    // Initialize SignalR connection
    this.signalRService.startConnection();

    // Subscribe to progress messages
    this.signalRService.addHandler('ReceiveProgress', (progress: string) => {
      this.progress = `${progress}`;
    });

    // Subscribe to error messages
    this.signalRService.addHandler('ReceiveError', (error: string) => {
      this.error += `${error}` + "\n\n";
    });

    // Subscribe to download finished
    this.signalRService.addHandler('DownloadFinished', (message: string) => {
      this.isDownloading = false;
      this.finish = `${message}`;
    });
    this.signalRService.addHandler('ReceiveState', (message: string) => {
      this.ReceiveState = `${message}`;
      if (message == 'Success') {
        this.isDownloading = false;
      }
    });
    this.signalRService.addHandler('ReceiveSpeed', (message: string) => {
      this.ReceiveSpeed = `${message}`;
    });
    this.signalRService.addHandler('ReceiveETA', (message: string) => {
      this.ReceiveETA = `${message}`;
    });
    this.signalRService.addHandler('ReceiveTotalSize', (message: string) => {
      this.ReceiveTotalSize = `${message}`;
    });
    this.signalRService.addHandler('ReceiveVideoIndex', (message: string) => {
      this.ReceiveVideoIndex = `${message}`;
    });
    this.signalRService.addHandler('ReceiveData', (message: string) => {
      this.ReceiveData = `${message}`;
    });

    
  }
  startDownload(): void {
    // Retrieve SignalR connection ID
    const connectionId = this.signalRService.getConnectionId();
    if (!connectionId) {
      alert('Connection to SignalR not established yet. Please wait...');
      return;
    }
    const payload = {
      downloadId: connectionId,
      url: this.url,
      options: this.options,
      audioOnly: this.audioOnly,
      outputFolder: this.outputFolder  // Send the user-provided output folder.
    };
    this.isDownloading = true;  
     this.signalRService.invokeMethod('HubStartDownloadServiceAsync', payload);
  }
}
