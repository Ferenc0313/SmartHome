using System.Threading;
using System.Threading.Tasks;

namespace SmartHomeUI.Services;

public interface ISmartThingsPollingService
{
    Task StartAsync(CancellationToken token = default);
    Task StopAsync();
}

