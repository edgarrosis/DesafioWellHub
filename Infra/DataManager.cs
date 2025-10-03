using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkOfflineCourse.Infra;

public class DataManager
{
    private readonly string _dataDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public DataManager(string dataDirectory = "data")
    {
        _dataDirectory = Path.GetFullPath(dataDirectory);
    }

    public async Task<CheckinRecord[]> GetCheckinRecordsAsync()
    {
        var filePath = Path.Combine(_dataDirectory, "checkin_records.json");
        if (!File.Exists(filePath))
        {
            return Array.Empty<CheckinRecord>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<CheckinData>(json, JsonOptions);
        return data?.CheckinRecords ?? Array.Empty<CheckinRecord>();
    }

    public async Task<Partner[]> GetPartnersAsync()
    {
        var filePath = Path.Combine(_dataDirectory, "partners.json");
        if (!File.Exists(filePath))
        {
            return Array.Empty<Partner>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<PartnersData>(json, JsonOptions);
        return data?.Partners ?? Array.Empty<Partner>();
    }

    public async Task<User[]> GetUsersAsync()
    {
        var filePath = Path.Combine(_dataDirectory, "users.json");
        if (!File.Exists(filePath))
        {
            return Array.Empty<User>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<UsersData>(json, JsonOptions);
        return data?.Users ?? Array.Empty<User>();
    }

    public async Task<CheckinRecord?> FindCheckinRecordAsync(string userId, string partnerId, string timestamp)
    {
        var records = await GetCheckinRecordsAsync();
        return records.FirstOrDefault(r => 
            r.UserId == userId && 
            r.PartnerId == partnerId && 
            r.Timestamp == timestamp);
    }

    public async Task<Partner?> FindPartnerAsync(string partnerId)
    {
        var partners = await GetPartnersAsync();
        return partners.FirstOrDefault(p => p.Id == partnerId);
    }

    public async Task<User?> FindUserAsync(string userId)
    {
        var users = await GetUsersAsync();
        return users.FirstOrDefault(u => u.Id == userId);
    }
}

// Classes de modelo para os dados JSON
public class CheckinData
{
    [JsonPropertyName("checkin_records")]
    public CheckinRecord[] CheckinRecords { get; set; } = Array.Empty<CheckinRecord>();
}

public class CheckinRecord
{
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("partnerId")]
    public string PartnerId { get; set; } = string.Empty;
    
    public string Timestamp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    
    [JsonPropertyName("partner_name")]
    public string PartnerName { get; set; } = string.Empty;
    
    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;
    
    public Location Location { get; set; } = new();
    
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }
    
    [JsonPropertyName("error_reason")]
    public string? ErrorReason { get; set; }
    
    [JsonPropertyName("discount_applied")]
    public bool? DiscountApplied { get; set; }
    
    [JsonPropertyName("original_amount")]
    public decimal? OriginalAmount { get; set; }
}

public class PartnersData
{
    public Partner[] Partners { get; set; } = Array.Empty<Partner>();
}

public class Partner
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("operating_hours")]
    public OperatingHours OperatingHours { get; set; } = new();
    
    public string[] Services { get; set; } = Array.Empty<string>();
    public bool Active { get; set; }
    
    [JsonPropertyName("closure_reason")]
    public string? ClosureReason { get; set; }
}

public class UsersData
{
    public User[] Users { get; set; } = Array.Empty<User>();
}

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    
    [JsonPropertyName("registration_date")]
    public string RegistrationDate { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("credit_balance")]
    public decimal CreditBalance { get; set; }
    
    [JsonPropertyName("monthly_limit")]
    public decimal MonthlyLimit { get; set; }
    
    [JsonPropertyName("preferred_activities")]
    public string[] PreferredActivities { get; set; } = Array.Empty<string>();
    
    public Location Location { get; set; } = new();
    
    [JsonPropertyName("payment_issue")]
    public PaymentIssue? PaymentIssue { get; set; }
    
    [JsonPropertyName("suspension_reason")]
    public string? SuspensionReason { get; set; }
}

public class Location
{
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Region { get; set; }
}

public class OperatingHours
{
    [JsonPropertyName("monday_friday")]
    public string MondayFriday { get; set; } = string.Empty;
    
    public string Saturday { get; set; } = string.Empty;
    public string Sunday { get; set; } = string.Empty;
}

public class PaymentIssue
{
    [JsonPropertyName("card_expired")]
    public bool CardExpired { get; set; }
    
    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; } = string.Empty;
}