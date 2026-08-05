import { useToast } from "../contexts/ToastContext";

export const useYouTubeService = () => {
  const { showSuccess, showInfo, showWarn, showError } = useToast();

  const openYouTubeVideo = (url: string) => {
    // Extract video ID from YouTube URL
    const videoId = extractYouTubeVideoId(url);
    if (!videoId) {
      showWarn("Invalid YouTube URL");
      return;
    }
    window.open(`https://www.youtube.com/watch?v=${videoId}`, "_blank");
  };

  const extractYouTubeVideoId = (url: string): string | null => {
    const regex =
      /(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([^&\n?#]+)/;
    const match = url.match(regex);
    return match ? match[1] : null;
  };

  return {
    openYouTubeVideo,
  };
};
