using System.Runtime.InteropServices;
using AudioSwap.Models;
using NAudio.CoreAudioApi;
using PROPERTYKEY = AudioSwap.Services.NativeMethods.PROPERTYKEY;

namespace AudioSwap.Services;

public sealed class AudioDeviceService : IDisposable
{
    private readonly MMDeviceEnumerator _deviceEnumerator;
    private readonly NativeMethods.IPolicyConfig _policyConfig;

    public AudioDeviceService()
    {
        _deviceEnumerator = new MMDeviceEnumerator();
        _policyConfig = (NativeMethods.IPolicyConfig)new NativeMethods.PolicyConfigClientComObject();
    }

    public IReadOnlyList<AudioDevice> GetPlaybackDevices()
    {
        return _deviceEnumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(device => new AudioDevice
            {
                Id = device.ID,
                Name = device.FriendlyName
            })
            .OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public string GetDefaultPlaybackDeviceId()
    {
        return _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
    }

    public void SetDefaultPlaybackDevice(string deviceId)
    {
        foreach (var role in new[]
                 {
                     NativeMethods.ERole.eConsole,
                     NativeMethods.ERole.eMultimedia,
                     NativeMethods.ERole.eCommunications
                 })
        {
            Marshal.ThrowExceptionForHR(_policyConfig.SetDefaultEndpoint(deviceId, role));
        }
    }

    public void Dispose()
    {
        Marshal.ReleaseComObject(_policyConfig);
        _deviceEnumerator.Dispose();
    }
}
