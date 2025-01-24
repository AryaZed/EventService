using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Models.ML
{
    public class EventPredictionData
    {
        [LoadColumn(0)] public float ProcessedUsers { get; set; }
        [LoadColumn(1)] public float SuccessCount { get; set; }
        [LoadColumn(2)] public float FailureCount { get; set; }
        [LoadColumn(3)] public float ProcessingDuration { get; set; }
        [LoadColumn(4)] public float EngagementScore { get; set; }
    }
}
