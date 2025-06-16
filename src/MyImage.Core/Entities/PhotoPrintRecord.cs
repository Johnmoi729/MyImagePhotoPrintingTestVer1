using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Record of when this photo was printed
/// </summary>
public class PhotoPrintRecord
{
    [BsonElement("orderId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("printedAt")]
    public DateTime PrintedAt { get; set; }

    [BsonElement("size")]
    public string Size { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; }
}