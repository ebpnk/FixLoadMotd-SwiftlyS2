using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Events;

namespace FixLoadMotd;

[PluginMetadata(Id = "FixLoadMotd", Version = "1.0.0", Name = "FixLoadMotd", Author = "DoctorishHD")]
public class Plugin : BasePlugin
{
    private readonly ILogger _logger;
    private INetworkStringTableContainer? _networkStringTableContainer;

    public static readonly int LINUX_OFFSET_PREDICT = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? 1 : 0;

    private const string INTERFACE_NAME = "Source2EngineToServerStringTable001";
    private const string TABLE_NAME = "InfoPanel";
    private const string STRING_KEY_NAME = "motd";

    public Plugin(ISwiftlyCore core) : base(core)
    {
        _logger = Core.LoggerFactory.CreateLogger<Plugin>();
    }

    public override void Load(bool hotReload)
    {
        _logger.LogInformation("FixLoadMotd plugin loaded");
    }

    [EventListener<EventDelegates.OnMapLoad>]
    public void OnMapLoad(IOnMapLoadEvent @event)
    {
        var motdConvar = Core.ConVar.Find<string>("motdfile");
        if (motdConvar == null)
        {
            _logger.LogError("motdfile convar not found");
            return;
        }

        string motdPath = Path.Combine(Core.CSGODirectory, motdConvar.Value ?? "motd.txt");

        if (!File.Exists(motdPath))
        {
            _logger.LogError("MOTD file not found at {motdPath}", motdPath);
            return;
        }

        // Handle async task properly to avoid unobserved exceptions
        Task.Run(async () =>
        {
            try
            {
                string url = (await File.ReadAllTextAsync(motdPath)).Trim();

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return;
                }

                await Core.Scheduler.NextTickAsync(() =>
                {
                    try
                    {
                        // Use SwiftlyS2's memory service to get the interface directly
                        nint? interfacePtr = Core.Memory.GetInterfaceByName(INTERFACE_NAME);
                        if (!interfacePtr.HasValue || interfacePtr.Value == 0)
                        {
                            _logger.LogError("Failed to get interface {interfaceName}", INTERFACE_NAME);
                            return;
                        }

                        _networkStringTableContainer ??= new(interfacePtr.Value);

                        if (_networkStringTableContainer == null)
                        {
                            _logger.LogError("Failed to create network string table container {interfaceName}", INTERFACE_NAME);
                            return;
                        }

                        INetworkStringTable? table = _networkStringTableContainer.FindTable(TABLE_NAME);

                        if (table == null)
                        {
                            _logger.LogError("Failed to find table {tableName}", TABLE_NAME);
                            return;
                        }

                        SetMOTDValue(table, url);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error setting MOTD value");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read MOTD file at {motdPath}", motdPath);
            }
        });
    }

    private unsafe void SetMOTDValue(INetworkStringTable table, string value)
    {
        var msg = Encoding.UTF8.GetBytes(value + "\0");

        fixed (byte* pMsg = msg)
        {
            SetStringUserDataRequest_t data;

            data.m_pRawData = pMsg;
            data.m_cbDataSize = msg.Length;

            if (table.AddString(true, STRING_KEY_NAME, ref data) != INetworkStringTable.INVALID_STRING_INDEX)
            {
                _logger.LogInformation("Successfully added MOTD string");
                return;
            }
        }
    }

    public override void Unload()
    {
        _logger.LogInformation("FixLoadMotd plugin unloaded");
    }
}