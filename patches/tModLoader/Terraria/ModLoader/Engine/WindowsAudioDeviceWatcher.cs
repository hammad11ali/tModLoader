using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Terraria.ModLoader.Engine {
	internal static class WindowsAudioDeviceWatcher {
		private static Thread _thread;
		private static volatile bool _running;
		private static IMMNotificationClient _client;
		private static MMDeviceEnumerator _enumerator;

		public static void Start() {
			if (_running || !OperatingSystem.IsWindows()) return;
			_running = true;
			_thread = new Thread(Run) { IsBackground = true, Name = "WindowsAudioDeviceWatcher" };
			_thread.Start();
		}

		private static void Run() {
			try {
				int hr = CoInitializeEx(IntPtr.Zero, 0x0); // COINIT_MULTITHREADED
				_enumerator = new MMDeviceEnumerator();
				_client = new NotificationClient();
				_enumerator.RegisterEndpointNotificationCallback(_client);
				while (_running) Thread.Sleep(500);
				_enumerator.UnregisterEndpointNotificationCallback(_client);
				CoUninitialize();
			} catch (Exception ex) {
				Logging.FNA.Warn($"Audio watcher failed: {ex}");
			}
		}

		private class NotificationClient : IMMNotificationClient {
			public void OnDefaultDeviceChanged(int flow, int role, string deviceId) {
				if (flow == 0) { // eRender
					Logging.FNA.Warn("Default audio device changed. Suspending audio to avoid crash.");
					AudioPanicHandler.SuspendAudio("Device Change");
				}
			}
			public void OnDeviceAdded(string id) { }
			public void OnDeviceRemoved(string id) { }
			public void OnDeviceStateChanged(string id, uint newState) { }
			public void OnPropertyValueChanged(string id, PropertyKey key) { }
		}

		[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IMMDeviceEnumerator {
			void EnumAudioEndpoints();
			void GetDefaultAudioEndpoint();
			void GetDevice();
			void RegisterEndpointNotificationCallback(IMMNotificationClient client);
			void UnregisterEndpointNotificationCallback(IMMNotificationClient client);
		}

		[ComImport, Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IMMNotificationClient {
			void OnDeviceStateChanged(string id, uint newState);
			void OnDeviceAdded(string id);
			void OnDeviceRemoved(string id);
			void OnDefaultDeviceChanged(int flow, int role, string deviceId);
			void OnPropertyValueChanged(string id, PropertyKey key);
		}

		[StructLayout(LayoutKind.Sequential)] private struct PropertyKey { public Guid fmtid; public int pid; }

		[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] private class MMDeviceEnumerator { }

		[DllImport("ole32.dll")] private static extern int CoInitializeEx(IntPtr pvReserved, uint coInit);
		[DllImport("ole32.dll")] private static extern void CoUninitialize();
	}
}