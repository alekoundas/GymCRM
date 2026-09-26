// Pictures people upload. Only everyday photo formats are taken: no SVG (a drawing
// that can carry script), no GIF, no oddities a browser might render differently.
export const ALLOWED_IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp"];
export const ALLOWED_IMAGE_ACCEPT = ALLOWED_IMAGE_TYPES.join(",");

export const isAllowedImageType = (file: File): boolean =>
  ALLOWED_IMAGE_TYPES.includes(file.type);

// Redraws the picture at no more than maxSide pixels on its longest edge and saves it
// as a JPEG. A phone photo of several megabytes comes out at a few tens of KB, which
// is what every page showing it has to load. Redrawing also drops everything that is
// not the picture itself - the camera details and GPS location a photo usually
// carries - along with anything hidden inside the file.
export const resizeToJpegDataUrl = (
  file: File,
  maxSide: number,
  quality = 0.85
): Promise<string> =>
  new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const image = new Image();

    image.onload = () => {
      const scale = Math.min(1, maxSide / Math.max(image.width, image.height));
      const width = Math.max(1, Math.round(image.width * scale));
      const height = Math.max(1, Math.round(image.height * scale));

      const canvas = document.createElement("canvas");
      canvas.width = width;
      canvas.height = height;

      const context = canvas.getContext("2d");
      if (!context) {
        URL.revokeObjectURL(url);
        reject(new Error("Canvas is not available"));
        return;
      }

      // JPEG has no transparency; a transparent PNG goes onto white, not black.
      context.fillStyle = "#ffffff";
      context.fillRect(0, 0, width, height);
      context.drawImage(image, 0, 0, width, height);

      URL.revokeObjectURL(url);
      resolve(canvas.toDataURL("image/jpeg", quality));
    };

    image.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error("Not a readable image"));
    };

    image.src = url;
  });
