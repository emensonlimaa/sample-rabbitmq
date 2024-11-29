using Microsoft.Extensions.Configuration;

namespace MessageBroker.Configuration;

public static class AppSettingsReader
{
    private static IConfigurationRoot _configuration;

    static AppSettingsReader()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) 
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) 
            .AddEnvironmentVariables() 
            .Build();
    }
    
    public static T GetSection<T>(string sectionName) where T : new()
    {
        var section = new T();
        _configuration.GetSection(sectionName).Bind(section); 
        return section;
    }
    
    public static string? GetValue(string key)
    {
        return _configuration[key];
    }
}