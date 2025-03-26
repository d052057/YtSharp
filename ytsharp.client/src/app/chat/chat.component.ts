import { NgFor } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

@Component({
  selector: 'app-chat',
  imports: [NgFor, FormsModule],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent implements OnInit {
  title = 'ChatAppClient';
  private connection: HubConnection;
  public messages: string[] = [];
  public user: string = "";
  public message: string = "";

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl("https://localhost:7217/chat")
      .build();
  }

  async ngOnInit() {
    this.connection.on('ReceiveMessage', (user, message) => {
      this.messages.push(`${user}: ${message}`);
    });

    try {
      await this.connection.start();
      console.log('Connected to SignalR hub');
      alert ('Connected to SignalR hub');
    } catch (error) {
      console.error('Failed to connect to SignalR hub', error);
      alert('Failed to connect to SignalR hub');
    }
  }

  async sendMessage() {
    if (!this.user || !this.message) return;
    await this.connection.invoke('SendMessage', this.user, this.message);
    this.message = '';
  }
}
