using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KanbanRedmine.Models
{
    /// <summary>
    /// Representa la respuesta de la API para las actividades de entrada de tiempo.
    /// </summary>
    public class TimeEntryActivitiesResponse
    {
        [JsonPropertyName("time_entry_activities")]
        public List<RedmineReference> TimeEntryActivities { get; set; } = new();
    }
}