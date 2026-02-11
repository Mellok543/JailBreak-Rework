using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.DependencyInjection;

namespace JailBreak;

public class JailBreakServiceCollection : IPluginServiceCollection<JailBreak>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        IFeature.Scan(serviceCollection);
    }
}