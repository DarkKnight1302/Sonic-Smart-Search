using System.IO;

namespace SonicExplorerLib.Utils
{
    public static class GlyphIconForFile
    {
        public static string GetGlyphIconClass(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            switch (extension)
            {
                case ".doc":
                case ".docx":
                    return "\uE8A5"; // Word document glyph
                case ".xls":
                case ".xlsx":
                    return "\uE8A6"; // Excel spreadsheet glyph
                case ".ppt":
                case ".pptx":
                    return "\uE8A7"; // PowerPoint presentation glyph
                case ".pdf":
                    return "\uE8A3"; // PDF file glyph
                case ".txt":
                    return "\uE8C4"; // Text document glyph
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                    return "\uE8B8"; // Image glyph
                default:
                    return "\uE8A0"; // Generic file glyph
            }
        }
    }
}
