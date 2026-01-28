using Courcework.Common;
using Courcework.Entities;
using System.Text.RegularExpressions;

namespace Courcework.Services
{
    
    /// Service for exporting journal entries to various formats
    
    public interface IExportService
    {
        Task<ServiceResult<byte[]>> ExportToPdfAsync(List<JournalEntry> entries, string title = "Journal Export");
        Task<ServiceResult<string>> ExportToJsonAsync(List<JournalEntry> entries);
    }

    public class ExportService : IExportService
    {
        
        /// Export entries to PDF format
        
        public async Task<ServiceResult<byte[]>> ExportToPdfAsync(List<JournalEntry> entries, string title = "Journal Export")
        {
            try
            {
                if (entries == null || entries.Count == 0)
                    return ServiceResult<byte[]>.Fail("No entries to export");

                // Create PDF content as HTML, which can be converted
                var htmlContent = GeneratePdfHtml(entries, title);
                
                // For now, return the HTML as bytes (requires HTML-to-PDF converter)
                // In production, use iTextSharp or similar library
                var bytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
                
                System.Diagnostics.Debug.WriteLine($"Exported {entries.Count} entries to PDF");
                
                return ServiceResult<byte[]>.Ok(bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF Export error: {ex.Message}");
                return ServiceResult<byte[]>.Fail(ex.Message);
            }
        }

        
        /// Export entries to JSON format
        
        public async Task<ServiceResult<string>> ExportToJsonAsync(List<JournalEntry> entries)
        {
            try
            {
                if (entries == null || entries.Count == 0)
                    return ServiceResult<string>.Fail("No entries to export");

                var json = System.Text.Json.JsonSerializer.Serialize(
                    entries.OrderByDescending(e => e.Date),
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );
                
                System.Diagnostics.Debug.WriteLine($"Exported {entries.Count} entries to JSON");
                
                return ServiceResult<string>.Ok(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Export error: {ex.Message}");
                return ServiceResult<string>.Fail(ex.Message);
            }
        }

        
        /// Generate HTML content for PDF
        
        private string GeneratePdfHtml(List<JournalEntry> entries, string title)
        {
            var html = new System.Text.StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #333; border-bottom: 2px solid #007bff; padding-bottom: 10px; }");
            html.AppendLine(".entry { margin: 20px 0; padding: 15px; border-left: 4px solid #007bff; background: #f9f9f9; }");
            html.AppendLine(".entry-date { font-weight: bold; color: #007bff; }");
            html.AppendLine(".entry-title { font-size: 18px; font-weight: bold; margin: 10px 0; }");
            html.AppendLine(".entry-mood { color: #666; margin: 5px 0; }");
            html.AppendLine(".entry-tags { color: #999; font-size: 12px; margin: 5px 0; }");
            html.AppendLine(".entry-content { margin: 10px 0; line-height: 1.6; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            html.AppendLine($"<h1>{title}</h1>");
            html.AppendLine($"<p>Exported on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"<p>Total Entries: {entries.Count}</p>");
            html.AppendLine("<hr>");
            
            foreach (var entry in entries.OrderByDescending(e => e.Date))
            {
                html.AppendLine("<div class=\"entry\">");
                html.AppendLine($"<div class=\"entry-date\">{entry.Date:MMMM dd, yyyy}</div>");
                
                
                if (!string.IsNullOrEmpty(entry.Title))
                    html.AppendLine($"<div class=\"entry-title\">{System.Web.HttpUtility.HtmlEncode(entry.Title)}</div>");
                
                if (entry.Mood.HasValue)
                    html.AppendLine($"<div class=\"entry-mood\"><strong>Mood:</strong> {entry.Mood.Value}{(!string.IsNullOrEmpty(entry.SecondaryMood) ? $", {entry.SecondaryMood}" : "")}</div>");
                
                if (entry.Tags.Count > 0)
                    html.AppendLine($"<div class=\"entry-tags\"><strong>Tags:</strong> {string.Join(", ", entry.Tags)}</div>");
                
                if (!string.IsNullOrEmpty(entry.Category))
                    html.AppendLine($"<div class=\"entry-mood\"><strong>Category:</strong> {entry.Category}</div>");
                
                var cleanContent = StripHtml(entry.Content);
                html.AppendLine($"<div class=\"entry-content\">{System.Web.HttpUtility.HtmlEncode(cleanContent)}</div>");
                
                
                html.AppendLine("</div>");
            }
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string StripHtml(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            var text = Regex.Replace(content, "<[^>]*>", " ");
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
    }
}
