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
        
        if (data.TryGetProperty("checkin_records", out var records))
        {
            var recordsList = new List<dynamic>();
            foreach (var record in records.EnumerateArray())
            {
                var recordDict = JsonSerializer.Deserialize<Dictionary<string, object>>(record.GetRawText());
                if (recordDict != null)
                    recordsList.Add(recordDict);
            }
            return recordsList;
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

    public async Task<bool> UpdateCheckinRecordAsync(string recordId, JsonElement updatedRecord)
    {
        try
        {
            var filePath = Path.Combine(_dataPath, "checkin_records.json");
            if (!File.Exists(filePath)) return false;

            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("checkin_records", out var records))
            {
                var recordsList = records.EnumerateArray().ToList();
                var updatedList = new List<JsonElement>();

                foreach (var record in recordsList)
                {
                    if (record.GetProperty("id").GetString() == recordId)
                    {
                        updatedList.Add(updatedRecord);
                    }
                    else
                    {
                        updatedList.Add(record);
                    }
                }

                var newData = new { checkin_records = updatedList };
                var updatedJson = JsonSerializer.Serialize(newData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(filePath, updatedJson);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateCheckinRecordStatusAsync(string userId, string partnerId, DateTime timestamp, string newStatus, string newDetails)
    {
        try
        {
            var filePath = Path.Combine(_dataPath, "checkin_records.json");
            if (!File.Exists(filePath)) return false;

            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("checkin_records", out var records))
            {
                var recordsList = records.EnumerateArray().ToList();
                var updatedList = new List<object>();
                bool recordUpdated = false;

                foreach (var record in recordsList)
                {
                    var recordObj = JsonSerializer.Deserialize<Dictionary<string, object>>(record.GetRawText());
                    
                    if (recordObj != null && 
                        recordObj.TryGetValue("userId", out var recUserId) && 
                        recordObj.TryGetValue("partnerId", out var recPartnerId) && 
                        recordObj.TryGetValue("timestamp", out var recTimestamp))
                    {
                        var recordUserId = recUserId?.ToString();
                        var recordPartnerId = recPartnerId?.ToString();
                        var recordTimestampStr = recTimestamp?.ToString();

                        if (recordUserId == userId && 
                            recordPartnerId == partnerId && 
                            recordTimestampStr != null &&
                            DateTime.TryParse(recordTimestampStr, out var recordDate) &&
                            recordDate.Date == timestamp.Date)
                        {
                            // Atualizar este registro
                            recordObj["status"] = newStatus;
                            recordObj["details"] = newDetails;
                            recordUpdated = true;
                        }
                    }

                    if (recordObj != null)
                        updatedList.Add(recordObj);
                }

                if (recordUpdated)
                {
                    var newData = new { checkin_records = updatedList };
                    var updatedJson = JsonSerializer.Serialize(newData, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    await File.WriteAllTextAsync(filePath, updatedJson);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}