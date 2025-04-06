using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageConsole
{
    public class FeedbackEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string User { get; set; }
        public string Type { get; set; }
        public DateTime Time { get; set; }
        public string Message { get; set; }
        public string Screenshot { get; set; }
        public string Response { get; set; }
        public string Status { get; set; } = "In Progress";
        public string Severity { get; set; }
    }
}
