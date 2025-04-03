export interface DownloadRequest {
  id: string;
  url: string;
  audioOnly: boolean;
  options: string;
  outputFolder: string;
};
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
