import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HubConnection, HubConnectionState, HubConnectionBuilder } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { DownloadStatus } from './youtube-dl.model';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private hubConnection: signalR.HubConnection;
  private progressSubject = new Subject<{ downloadId: string; status: DownloadStatus }>();

  public progress$ = this.progressSubject.asObservable();
  private apiHub = '/downloadHub';
  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.apiHub) // Match the SignalR hub URL on the server
      .withAutomaticReconnect()
      .build();
  }

  startConnection() {
    this.hubConnection
      .start()
      .then(() => console.log('SignalR connection started'))
      .catch((err) => console.error('Error starting SignalR connection:', err));

    // Listen for progress updates from the server
    this.hubConnection.on('ReceiveProgress', (downloadId: string, status: DownloadStatus) => {
      this.progressSubject.next({ downloadId, status });
      //console.log("downloadid:" + downloadId);
      //console.log("status:" + JSON.stringify(status))
    });
  }
}
