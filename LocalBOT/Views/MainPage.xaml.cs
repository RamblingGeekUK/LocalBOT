using System.Diagnostics;
using LocalBOT.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types.Events;
using OBSWebsocketDotNet.Types;
using Xamarin.Essentials;

namespace LocalBOT.Views;

public partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    protected OBSWebsocket obs;

    private CancellationTokenSource keepAliveTokenSource;
    private readonly int keepAliveInterval = 500;

    bool VirtualCamStarted=false;

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();

        obs = new OBSWebsocket();
        obs.Connected += onConnect;
        obs.Disconnected += onDisconnect;

        obs.VirtualcamStateChanged += onVirtualCamStateChanged;
    }

    private void onDisconnect(object? sender, ObsDisconnectionInfo e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (keepAliveTokenSource != null)
            {
                keepAliveTokenSource.Cancel();
            }
            //gbControls.Enabled = false;

            //txtServerIP.Enabled = true;
            //txtServerPassword.Enabled = true;
            btnConnect.Content = "Connect";

            if (e.ObsCloseCode == OBSWebsocketDotNet.Communication.ObsCloseCodes.AuthenticationFailed)
            {
                InfoBar.IsOpen = true;
                InfoBar.Severity = InfoBarSeverity.Warning;
                InfoBar.Message = "Autentication failed";
                
                return;
            }
            else if (e.WebsocketDisconnectionInfo != null)
            {
                if (e.WebsocketDisconnectionInfo.Exception != null)
                {
                    //MessageBox.Show($"Connection failed: CloseCode: {e.ObsCloseCode} Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription} Exception:{e.WebsocketDisconnectionInfo?.Exception?.Message}\nType: {e.WebsocketDisconnectionInfo.Type}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    InfoBar.IsOpen = true;
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.Message = $"Connection failed: CloseCode: {e.ObsCloseCode} Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription} Exception:{e.WebsocketDisconnectionInfo?.Exception?.Message}\nType: {e.WebsocketDisconnectionInfo.Type}";
                }
                else
                {
                    //MessageBox.Show($"Connection failed: CloseCode: {e.ObsCloseCode} Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription}\nType: {e.WebsocketDisconnectionInfo.Type}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    InfoBar.IsOpen = true;
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.Message = $"Connection failed: CloseCode: {e.ObsCloseCode} Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription} Exception:{e.WebsocketDisconnectionInfo?.Exception?.Message}\nType: {e.WebsocketDisconnectionInfo.Type}";                    
                }
            }
            else
            {
                //MessageBox.Show($"Connection failed: CloseCode: {e.ObsCloseCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                InfoBar.IsOpen = true;
                InfoBar.Severity = InfoBarSeverity.Error;
                InfoBar.Message = $"Connection failed: CloseCode: {e.ObsCloseCode}";

                return;
            }

        });
    }
    private void onConnect(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var versionInfo = obs.GetVersion();
            OBSVersion.Text = $"OBS Version : {versionInfo.OBSStudioVersion.ToString()}";
            OBSWSVersion.Text = $"OBS WS Version : {versionInfo.PluginVersion.ToString()}";
        });
    }

    private void Button_ClickAsync(object sender, RoutedEventArgs e)
    {

        if (!obs.IsConnected)
        {

            try
            {
                obs.ConnectAsync(txtServerIP.Text, txtServerPassword.Password);
                InfoBar.IsOpen = true;
                InfoBar.Severity = InfoBarSeverity.Success;
                InfoBar.Message = "Connected to OBS";
                btnConnect.Content = "Disconnect";
            }
            catch (Exception)
            {
                InfoBar.IsOpen = true;
                InfoBar.Severity = InfoBarSeverity.Error;
                InfoBar.Message = "Failed to connect to OBS";
            }
        }
        else
        {
            obs.Disconnect();
            btnConnect.Content = "Connect";

        }
    }

    private void Button_VirtualCamStart_Click(object sender, RoutedEventArgs e)
    {
        obs.ToggleVirtualCam();
    }

    private void onVirtualCamStateChanged(object sender, VirtualcamStateChangedEventArgs args)
    {
        var state = "";
        var enabled = false;
        switch (args.OutputState.State)
        {
            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
                state = "VirtualCam starting...";
                break;

            case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                state = "VirtualCam Started";
                enabled = true;
                break;

            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                state = "VirtualCam stopping...";
                break;

            case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                state = "VirtualCam Stopped";
                enabled = false;
                break;

            default:
                state = "State unknown";
                break;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            lblVirtualCamStatus.Text = state;
            togVirtualCam.Content = state;
            togVirtualCam.IsChecked = enabled;

        });
    }


}
