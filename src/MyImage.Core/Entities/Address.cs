using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Address entity embedded within User documents for shipping and billing
/// Supports multiple addresses per user with default designation
/// Used for order fulfillment and payment processing
/// </summary>
public class Address
{
    /// <summary>
    /// Unique identifier for this address within the user's collection
    /// Allows easy reference and updates to specific addresses
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// Address type designation (shipping, billing)
    /// Helps organize addresses by their intended use
    /// </summary>
    [BsonElement("type")]
    public string Type { get; set; } = "shipping";

    /// <summary>
    /// User-friendly label for easy identification
    /// Examples: "Home", "Work", "Mom's House"
    /// </summary>
    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the user's default address for this type
    /// Simplifies checkout process by pre-selecting common addresses
    /// </summary>
    [BsonElement("isDefault")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Full name of the recipient at this address
    /// May differ from user's name (gifts, workplace deliveries)
    /// </summary>
    [Required]
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Company name for business deliveries
    /// Optional field for workplace or business addresses
    /// </summary>
    [BsonElement("company")]
    public string? Company { get; set; }

    /// <summary>
    /// Primary street address line
    /// Contains street number, name, and primary location info
    /// </summary>
    [Required]
    [BsonElement("streetLine1")]
    public string StreetLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Secondary address information
    /// Apartment numbers, suite numbers, floor information
    /// </summary>
    [BsonElement("streetLine2")]
    public string? StreetLine2 { get; set; }

    /// <summary>
    /// City name for address validation and shipping calculations
    /// </summary>
    [Required]
    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province for regional shipping and tax calculations
    /// </summary>
    [Required]
    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal code for precise delivery and shipping cost calculation
    /// </summary>
    [Required]
    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country code for international shipping support
    /// </summary>
    [Required]
    [BsonElement("country")]
    public string Country { get; set; } = "USA";

    /// <summary>
    /// Contact phone number for delivery notifications
    /// Carrier may use for delivery coordination
    /// </summary>
    [BsonElement("phone")]
    public string? Phone { get; set; }
}