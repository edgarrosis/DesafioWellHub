using System.Text.Json;

namespace SkOfflineCourse.Infra;

public class DataManager
{
    private readonly JsonMemoryStore _store;
    private readonly string _dataPath;

    public DataManager()
    {
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        _store = new JsonMemoryStore(_dataPath);
    }

    public async Task<dynamic?> FindCheckinRecordAsync(string userId, string partnerId, DateTime timestamp)
    {
        var checkinRecords = await GetCheckinRecordsAsync();
        return checkinRecords.FirstOrDefault(r => 
            r.userId == userId && 
            r.partnerId == partnerId && 
            DateTime.Parse(r.timestamp.ToString()).Date == timestamp.Date);
    }

    public async Task<List<dynamic>> GetCheckinRecordsAsync()
    {
        var filePath = Path.Combine(_dataPath, "checkin_records.json");
        if (!File.Exists(filePath)) return new List<dynamic>();
        
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        
        if (data.TryGetProperty("checkinRecords", out var records))
        {
            return JsonSerializer.Deserialize<List<dynamic>>(records.GetRawText()) ?? new List<dynamic>();
        }
        
        return new List<dynamic>();
    }

    public async Task<List<dynamic>> GetUsersAsync()
    {
        var filePath = Path.Combine(_dataPath, "users.json");
        if (!File.Exists(filePath)) return new List<dynamic>();
        
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        
        if (data.TryGetProperty("users", out var users))
        {
            return JsonSerializer.Deserialize<List<dynamic>>(users.GetRawText()) ?? new List<dynamic>();
        }
        
        return new List<dynamic>();
    }

    public async Task<List<dynamic>> GetPartnersAsync()
    {
        var filePath = Path.Combine(_dataPath, "partners.json");
        if (!File.Exists(filePath)) return new List<dynamic>();
        
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        
        if (data.TryGetProperty("partners", out var partners))
        {
            return JsonSerializer.Deserialize<List<dynamic>>(partners.GetRawText()) ?? new List<dynamic>();
        }
        
        return new List<dynamic>();
    }

    public async Task<dynamic?> FindUserAsync(string userId)
    {
        var users = await GetUsersAsync();
        return users.FirstOrDefault(u => u.id == userId);
    }

    public async Task<dynamic?> FindPartnerAsync(string partnerId)
    {
        var partners = await GetPartnersAsync();
        return partners.FirstOrDefault(p => p.id == partnerId);
    }
}