using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KanbanRedmine.Models
{
    public class TrackersResponse
    {
        [JsonPropertyName("trackers")]
        public List<RedmineReference> Trackers { get; set; } = new();
    }
}