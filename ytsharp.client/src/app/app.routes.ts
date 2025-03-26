import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: 'home',
    loadComponent: () => import('../app/home/home.component')
      .then(mod => mod.HomeComponent)
  },
  {
    path: 'youtubedl',
    loadComponent: () => import('../app/youtube-dl/youtube-dl.component')
      .then(mod => mod.YoutubeDlComponent)
  },
  {
    path: 'chat',
    loadComponent: () => import('../app/chat/chat.component')
      .then(mod => mod.ChatComponent)
  }
];


