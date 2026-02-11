using System.Reflection;
using CounterStrikeSharp.API;
using Microsoft.Extensions.DependencyInjection;

namespace JailBreak;

public interface IFeature
{
    private static List<Type> _features = new();

    public static void Scan(IServiceCollection collection)
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => !t.IsAbstract &&
                                 !t.IsInterface))
        {
            if (type.IsAssignableTo(typeof(IFeature)))
            {
                collection.AddSingleton(type);
                _features.Add(type);
                Server.PrintToConsole("Singleton: " + type.Name);
            }
            else if (type.IsAssignableTo(typeof(IFeatureTransit)))
            {
                collection.AddTransient(type);
                Server.PrintToConsole("Transient: " + type.Name);
            }
        }
    }

    public static void Instantiate(IServiceProvider provider)
    {
        foreach (var feature in _features)
        {
            provider.GetRequiredService(feature);
        }

        _features = null!;
    }
}