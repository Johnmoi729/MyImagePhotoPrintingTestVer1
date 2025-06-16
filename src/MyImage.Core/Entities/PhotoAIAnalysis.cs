using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// AI-powered image analysis results
/// </summary>
public class PhotoAIAnalysis
{
    [BsonElement("sceneType")]
    public List<string> SceneType { get; set; } = new();

    [BsonElement("dominantColors")]
    public List<string> DominantColors { get; set; } = new();

    [BsonElement("facesDetected")]
    public int FacesDetected { get; set; } = 0;

    [BsonElement("quality")]
    public ImageQuality? Quality { get; set; }
}