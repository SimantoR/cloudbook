using System;

namespace CloudBook.API.Data
{
    public class ErrorLog
    {
        public string Exception { get; set; }
        public DateTime Time { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }

        public ErrorLog()
        {

        }

        public ErrorLog(Exception ex)
        {
            this.Exception = ex.Message;
            this.Time = DateTime.UtcNow;
            this.Source = ex.Source;
            this.StackTrace = ex.StackTrace;
        }
    }
}
