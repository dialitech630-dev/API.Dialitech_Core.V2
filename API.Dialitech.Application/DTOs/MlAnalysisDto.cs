using System.Text.Json.Serialization;

namespace API.Dialitech.Application.DTOs;

// ── ML Service Request ──────────────────────────────────────────────

public class MlAnalysisRequest
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("windowSize")]
    public int WindowSize { get; set; } = 12;

    [JsonPropertyName("readings")]
    public List<MlReadingDto> Readings { get; set; } = [];
}

public class MlReadingDto
{
    [JsonPropertyName("heartRate")]
    public double HeartRate { get; set; }

    [JsonPropertyName("oxygen")]
    public double Oxygen { get; set; }

    [JsonPropertyName("activity")]
    public double Activity { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

// ── ML Service Response ─────────────────────────────────────────────

public class MlAnalysisResponse
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("modelVersion")]
    public string ModelVersion { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("riskPrediction")]
    public MlRiskPrediction RiskPrediction { get; set; } = new();

    [JsonPropertyName("trendAnalysis")]
    public MlTrendAnalysis TrendAnalysis { get; set; } = new();

    [JsonPropertyName("patternDetection")]
    public MlPatternDetection PatternDetection { get; set; } = new();

    [JsonPropertyName("anomalyDetection")]
    public MlAnomalyDetection AnomalyDetection { get; set; } = new();
}

public class MlRiskPrediction
{
    [JsonPropertyName("riskScore")]
    public double RiskScore { get; set; }

    [JsonPropertyName("riskLevel")]
    public string RiskLevel { get; set; } = string.Empty;

    [JsonPropertyName("riskFactors")]
    public List<string> RiskFactors { get; set; } = [];

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;

    [JsonPropertyName("modelVersion")]
    public string ModelVersion { get; set; } = string.Empty;
}

public class MlTrendAnalysis
{
    [JsonPropertyName("heartRate")]
    public TrendInfo HeartRate { get; set; } = new();

    [JsonPropertyName("oxygen")]
    public TrendInfo Oxygen { get; set; } = new();

    [JsonPropertyName("activity")]
    public TrendInfo Activity { get; set; } = new();
}

public class TrendInfo
{
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("slope")]
    public double Slope { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("currentValue")]
    public double CurrentValue { get; set; }

    [JsonPropertyName("meanValue")]
    public double MeanValue { get; set; }
}

public class MlPatternDetection
{
    [JsonPropertyName("patternsFound")]
    public bool PatternsFound { get; set; }

    [JsonPropertyName("patterns")]
    public List<PatternInfo> Patterns { get; set; } = [];

    [JsonPropertyName("insights")]
    public List<string> Insights { get; set; } = [];
}

public class PatternInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public class MlAnomalyDetection
{
    [JsonPropertyName("anomalyDetected")]
    public bool AnomalyDetected { get; set; }

    [JsonPropertyName("anomalyScore")]
    public double AnomalyScore { get; set; }

    [JsonPropertyName("threshold")]
    public double Threshold { get; set; }

    [JsonPropertyName("affectedMetrics")]
    public List<string> AffectedMetrics { get; set; } = [];

    [JsonPropertyName("insights")]
    public List<string> Insights { get; set; } = [];
}
