import { useToast } from "../contexts/ToastContext";
import { useTranslator } from "./TranslatorService";

export const useYouTubeService = () => {
  const { t } = useTranslator();
  const { showSuccess, showInfo, showWarn, showError } = useToast();

  const openYouTubeVideo = (url: string) => {
    // Extract video ID from YouTube URL
    const videoId = extractYouTubeVideoId(url);
    if (!videoId) {
      showWarn("Invalid YouTube URL");
      return;
    }

    // Detect mobile (you can refine this with userAgent for more accuracy)
    const isMobile =
      window.matchMedia("(max-width: 768px)").matches ||
      /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(
        navigator.userAgent
      );

    if (isMobile) {
      // Try to open YouTube app (cross-platform scheme)
      const appUrl = `vnd.youtube://watch?v=${videoId}`;
      const webUrl = `https://www.youtube.com/watch?v=${videoId}`;

      // Attempt app open, fallback to web after a short delay if it fails
      const appLink = document.createElement("a");
      appLink.href = appUrl;
      appLink.style.display = "none";
      document.body.appendChild(appLink);
      appLink.click();
      document.body.removeChild(appLink);

      // Fallback: If app doesn't open (no error event, but we can timeout)
      setTimeout(() => {
        window.open(webUrl, "_blank");
      }, 1000); // Adjust timeout as needed; this assumes app launch is quick
    } else {
      // Desktop: Open in new tab
      window.open(`https://www.youtube.com/watch?v=${videoId}`, "_blank");
    }
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
