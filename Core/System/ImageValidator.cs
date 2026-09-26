namespace Core.System
{
    // Pictures members and staff upload are stored as base64 in the database and sent
    // back to every page that shows them. This checks one is really a picture we allow,
    // by its first bytes rather than whatever the upload claimed to be, and that it is
    // not so big it slows every page it appears on.
    public static class ImageValidator
    {
        public static bool IsAllowed(string? base64OrDataUrl, int maxBytes)
        {
            if (string.IsNullOrWhiteSpace(base64OrDataUrl))
                return true;

            // A data url carries its payload after the comma.
            string base64 = base64OrDataUrl;
            int comma = base64.IndexOf(',');
            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                base64 = base64.Substring(comma + 1);

            // Decoded size, worked out before decoding anything.
            if ((long)base64.Length * 3 / 4 > maxBytes + 3)
                return false;

            byte[] buffer = new byte[base64.Length];
            if (!Convert.TryFromBase64String(base64, buffer, out int length) || length == 0 || length > maxBytes)
                return false;

            return IsJpeg(buffer, length) || IsPng(buffer, length) || IsWebp(buffer, length);
        }

        // The same check for a picture that arrived already decoded.
        public static bool IsAllowed(byte[]? bytes, int maxBytes)
        {
            if (bytes == null || bytes.Length == 0)
                return true;

            return bytes.Length <= maxBytes
                && (IsJpeg(bytes, bytes.Length) || IsPng(bytes, bytes.Length) || IsWebp(bytes, bytes.Length));
        }

        private static bool IsJpeg(byte[] b, int n) =>
            n > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

        private static bool IsPng(byte[] b, int n) =>
            n > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
                  && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A;

        // "RIFF" .... "WEBP"
        private static bool IsWebp(byte[] b, int n) =>
            n > 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
                   && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50;
    }
}
