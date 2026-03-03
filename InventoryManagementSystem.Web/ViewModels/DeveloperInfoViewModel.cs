using System.Collections.Generic;

namespace InventoryManagementSystem.Web.ViewModels
{
    public class DeveloperInfoViewModel
    {
        public string Name { get; set; } = "Your Name Here";
        public string Title { get; set; } = "Full Stack Developer";
        public string Bio { get; set; } = "Passionate developer building innovative inventory management solutions.";
        public string ProfileImageUrl { get; set; } = "/images/developer-profile.jpg";
        public string Email { get; set; } = "developer@inventory.com";
        public string Phone { get; set; } = "+63 912 345 6789";
        public string Location { get; set; } = "Philippines";

        // Social Media Links
        public string FacebookUrl { get; set; } = "https://facebook.com/yourprofile";
        public string LinkedInUrl { get; set; } = "https://linkedin.com/in/yourprofile";
        public string GitHubUrl { get; set; } = "https://github.com/yourprofile";
        public string TwitterUrl { get; set; } = "https://twitter.com/yourprofile";

        // Skills
        public List<string> Skills { get; set; } = new List<string>
        {
            "C# / .NET Core",
            "ASP.NET MVC",
            "Entity Framework",
            "SQL Server",
            "JavaScript",
            "HTML5/CSS3",
            "Bootstrap 5",
            "SignalR"
        };

        // Project Stats
        public int ProjectsCompleted { get; set; } = 5;
        public int YearsExperience { get; set; } = 2;
        public int LinesOfCode { get; set; } = 15000;
        public int HappyClients { get; set; } = 3;

        // Timeline
        public List<TimelineItem> Timeline { get; set; } = new List<TimelineItem>
        {
            new TimelineItem { Year = "2024", Title = "Inventory Management System", Description = "Complete inventory solution with chat & calculator" },
            new TimelineItem { Year = "2023", Title = "First ASP.NET Project", Description = "Started journey with .NET development" }
        };
    }

    public class TimelineItem
    {
        public string Year { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}