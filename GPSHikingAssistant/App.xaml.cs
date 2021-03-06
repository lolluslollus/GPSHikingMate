﻿using LolloGPS.Data;
using LolloGPS.Data.Constants;
using LolloGPS.Data.Runtime;
using LolloGPS.Suspension;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Utilz;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Phone.Devices.Notification;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641
// decent get started tutorial: http://msdn.microsoft.com/en-us/library/windows/apps/Hh986968.aspx






namespace LolloGPS.Core
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public sealed partial class App : Application, IDisposable
	{
		private static PersistentData _persistentData = null; // PersistentData.GetInstance();
		public static PersistentData PersistentData { get { return _persistentData; } }
		private static Data.Runtime.RuntimeData _myRuntimeData = null; // RuntimeData.GetInstance();
		public static Data.Runtime.RuntimeData MyRuntimeData { get { return _myRuntimeData; } }



		#region construct and dispose
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{



			InitializeComponent();
			AddEventHandlers();
		}

		private static void InitData()
		{
			_persistentData = PersistentData.GetInstance();
			_myRuntimeData = RuntimeData.GetInstance();

			PersistentData.OpenTileCacheDb();
			PersistentData.OpenMainDb();

			_myRuntimeData.Activate();
		}
		public void Dispose()
		{
			RemoveEventHandlers();
		}
		#endregion construct and dispose

		#region event handlers
		private bool isEventHandlersActive = false;
		private void AddEventHandlers()
		{
			if (!isEventHandlersActive)
			{
				Resuming += OnResuming;
				Suspending += OnSuspending;
				UnhandledException += OnApp_UnhandledException;
				isEventHandlersActive = true;
			}
		}
		private void RemoveEventHandlers()
		{
			Resuming -= OnResuming;
			Suspending -= OnSuspending;
			UnhandledException -= OnApp_UnhandledException;
			isEventHandlersActive = false;
		}
		private async void OnApp_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			// this didn't work when the telephone force-shuts the app, it might work now that the call is async
			await Logger.AddAsync("App quitting on UnhandledException: " + e.Exception.ToString(), Logger.AppExceptionLogFilename);
			RemoveEventHandlers();
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used when the application is launched to open a specific file, to display
		/// search results, and so forth.
		/// This is also invoked when the app is resumed after being terminated.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected async override void OnLaunched(LaunchActivatedEventArgs e)
		{
			Logger.Add_TPL("OnLaunched()", Logger.ForegroundLogFilename, Logger.Severity.Info);

			InitData();
			if (!await Licenser.CheckLicensedAsync() /*|| _myRuntimeData.IsBuying*/) return;

			try
			{
				Task restore = Task.Run(delegate
				{
					return SuspensionManager.LoadSettingsAndDbDataAsync();
				});

				Frame rootFrame = GetCreateRootFrame(e);

				NavigateToRootFrameContent(rootFrame);

				// Ensure the current window is active
				Window.Current.Activate();
			}
			catch (Exception ex)
			{
				await Logger.AddAsync(ex.ToString(), Logger.ForegroundLogFilename).ConfigureAwait(false);
			}
		}

		/// <summary>










		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private async void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			await CloseAll();
			deferral.Complete();
		}

		/// <summary>
		/// Invoked when the app is resumed without being terminated.
		/// You should handle the Resuming event only if you need to refresh any displayed content that might have changed while the app is suspended. 
		/// You do not need to restore other app state when the app resumes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void OnResuming(object sender, object e)
		{
			Logger.Add_TPL("OnResuming()", Logger.ForegroundLogFilename, Logger.Severity.Info);

			InitData();
			if (!await Licenser.CheckLicensedAsync() /*|| _myRuntimeData.IsBuying*/) return;

			if (IsRootFrameMain)
			{
				Main main = (Window.Current.Content as Frame).Content as Main;
				// Settings and data are already in.
				// However, reread the history coz the background task may have changed it while I was suspended.
				RuntimeData.SetIsDBDataRead_UI(false);
				Logger.Add_TPL("OnResuming() called RuntimeData.SetIsDBDataRead_UI(false)", Logger.ForegroundLogFilename, Logger.Severity.Info);
				await PersistentData.LoadHistoryFromDbAsync(false);
				RuntimeData.SetIsDBDataRead_UI(true);
				Logger.Add_TPL("OnResuming() called RuntimeData.SetIsDBDataRead_UI(true)", Logger.ForegroundLogFilename, Logger.Severity.Info);
				// reregister events
				main.OnResuming();
				// In simple cases, I don't need to deregister events when suspending and reregister them when resuming, 
				// but I deregister them when suspending to make sure long running tasks are really stopped.
				// This also includes the background task state check.
				// If I stop registering and deregistering events, I must explicitly check for the background state in GPSInteractor, 
				// which may have changed when the app was suspended. For example, the user barred this app running in background while the app was suspended.
			}
			Logger.Add_TPL("OnResuming() ended", Logger.ForegroundLogFilename, Logger.Severity.Info);
		}
		//protected override void OnFileOpenPickerActivated(FileOpenPickerActivatedEventArgs args) // this one never fires...
		//{
		//    Logger.Add("App.xaml.cs.OnFileOpenPickerActivated() starting", Logger.ForegroundLogFilename);
		//    base.OnFileOpenPickerActivated(args);
		//}

		/// <summary>
		/// Fires when attempting to open a file, which is associated with the application. Test it with the app running and closed.
		/// </summary>
		protected override async void OnFileActivated(FileActivatedEventArgs e)
		{
			Logger.Add_TPL("App.xaml.cs.OnFileActivated() starting with kind = " + e.Kind.ToString() + " and previous execution state = " + e.PreviousExecutionState.ToString(),
				Logger.ForegroundLogFilename + " and verb = " + e.Verb,
				Logger.Severity.Info);

			try
			{
				InitData();
				if (!await Licenser.CheckLicensedAsync() /*|| _myRuntimeData.IsBuying*/) return;

				if (e?.Files?[0]?.Path?.Length > 4 && e.Files[0].Path.EndsWith(ConstantData.GPX_EXTENSION))
				{
					bool isAppAlreadyRunning = IsRootFrameMain;

					Frame rootFrame = GetCreateRootFrame(e);
					if (!isAppAlreadyRunning)
					{
						NavigateToRootFrameContent(rootFrame);
						Window.Current.Activate();
					}

					List<PersistentData.Tables> whichTables = null;
					var fileOpenPage = rootFrame.Content as IFileActivatable;
					if (fileOpenPage != null)
					{
						try
						{
							RuntimeData.SetIsDBDataRead_UI(false);
							if (isAppAlreadyRunning)
							{
								Logger.Add_TPL("OnFileActivated() is about to open a file, app already running", Logger.ForegroundLogFilename, Logger.Severity.Info);

								whichTables = await Task.Run(delegate { return fileOpenPage.LoadFileIntoDbAsync(e as FileActivatedEventArgs); });
								if (whichTables != null)
								{
									// get file data from DB into UI
									foreach (var series in whichTables)
									{
										await PersistentData.LoadSeriesFromDbAsync(series);
									}
									// centre view on the file data
									if (whichTables.Count > 0 && rootFrame?.Content as Main != null)
									{
										Main main = rootFrame.Content as Main;
										if (whichTables[0] == PersistentData.Tables.Landmarks)
										{
											Task centreView = main.MyVM.CentreOnLandmarksAsync();
										}
										else if (whichTables[0] == PersistentData.Tables.Route0)
										{
											Task centreView = main.MyVM.CentreOnRoute0Async();
										}
									}
								}
							}
							else
							{
								Logger.Add_TPL("OnFileActivated() is about to open a file, app not running", Logger.ForegroundLogFilename, Logger.Severity.Info);

								whichTables = await Task.Run(delegate { return fileOpenPage.LoadFileIntoDbAsync(e as FileActivatedEventArgs); }).ConfigureAwait(false);
								// get all data from DB into UI
								await SuspensionManager.LoadSettingsAndDbDataAsync().ConfigureAwait(false);

								// centre view on the file data
								if (whichTables != null && whichTables.Count > 0)
								{
									await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
									{
										if (whichTables?.Count > 0 && rootFrame?.Content as Main != null)
										{
											Main main = rootFrame.Content as Main;
											if (whichTables[0] == PersistentData.Tables.Landmarks)
											{
												Task centreView = main.MyVM.CentreOnLandmarksAsync();
											}
											else if (whichTables[0] == PersistentData.Tables.Route0)
											{
												Task centreView = main.MyVM.CentreOnRoute0Async();
											}
										}
									}).AsTask().ConfigureAwait(false);
								}
							}
						}
						catch (Exception ex)
						{
							await Logger.AddAsync(ex.ToString(), Logger.ForegroundLogFilename).ConfigureAwait(false);
						}
						finally
						{
							RuntimeData.SetIsDBDataRead_UI(true);
						}
					}
				}
				else
				{
					//when opening a lol file, which is required for the log, the application starts and crashes back "elegantly". Very unlikely anyway, because it is not an ordinary extension.
				}
			}
			catch (Exception ex)
			{
				await Logger.AddAsync(ex.ToString(), Logger.ForegroundLogFilename).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Fires when returning from a FileOpenPicker, a FileSavePicker etc.
		/// </summary>
		protected override async void OnActivated(IActivatedEventArgs e)
		{
			Logger.Add_TPL("App.xaml.cs.OnActivated() starting with kind = " + e.Kind.ToString() + " and previous execution state = " + e.PreviousExecutionState.ToString(),
				Logger.ForegroundLogFilename,
				Logger.Severity.Info);

			InitData();
			if (!await Licenser.CheckLicensedAsync() /*|| _myRuntimeData.IsBuying*/) return;

			try
			{
				//base.OnActivated(e); //no need
				var continuationArgs = e as IContinuationActivatedEventArgs;

				Frame rootFrame = GetCreateRootFrame(e);

				NavigateToRootFrameContent(rootFrame);

				Window.Current.Activate(); // was at the end

				// we don't need this, the data has been read already!
				//await RestoreDataAsync(e.PreviousExecutionState, isLoadRoute0, isLoadLandmarks).ConfigureAwait(false);

				//Check if this is a continuation, and what sort.
				if (continuationArgs != null)
				{
					Task fileOperation = null;
					switch (continuationArgs.Kind)
					{
						case ActivationKind.PickFileContinuation:
							var fileOpenPickerPage = rootFrame.Content as IFileOpenPickerContinuable;
							if (fileOpenPickerPage != null) fileOperation = fileOpenPickerPage.ContinueFileOpenPickerAsync(e as FileOpenPickerContinuationEventArgs);
							break;
						case ActivationKind.PickSaveFileContinuation:
							var fileSavePickerPage = rootFrame.Content as IFileSavePickerContinuable;
							if (fileSavePickerPage != null) fileOperation = fileSavePickerPage.ContinueFileSavePickerAsync(e as FileSavePickerContinuationEventArgs);
							break;
						//case ActivationKind.PickFolderContinuation:
						//    var folderPickerPage = rootFrame.Content as IFolderPickerContinuable;
						//    if (folderPickerPage != null) folderPickerPage.ContinueFolderPicker(e as FolderPickerContinuationEventArgs);
						//    break;
						//case ActivationKind.WebAuthenticationBrokerContinuation:
						//    var wabPage = rootFrame.Content as IWebAuthenticationContinuable;
						//    if (wabPage != null) wabPage.ContinueWebAuthentication(e as WebAuthenticationBrokerContinuationEventArgs);
						//    break;
						//case ActivationKind.Launch:
						//    Debugger.Break();
						//    break;
						//case ActivationKind.Protocol:
						//    Debugger.Break();
						//    break;
						//case ActivationKind.File: //it does not fire, use the dedicated method instead
						default:
							break;
					}
				}
			}
			catch (Exception ex)
			{
				await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
			}
		}
		#endregion event handlers

		#region services
		public async Task Quit()
		{
			await CloseAll();
			Exit();
		}
		private async Task CloseAll()
		{
			Logger.Add_TPL("CloseAll() started", Logger.ForegroundLogFilename, Logger.Severity.Info);

			// unregister events and stop long running tasks.
			if (IsRootFrameMain)
			{
				Main main = (Window.Current.Content as Frame).Content as Main;
				main.Deactivate();
			}
			// back up the app settings
			await SuspensionManager.SaveSettingsAsync(PersistentData).ConfigureAwait(false);
			// lock the DBs
			// await PersistentData.CloseMainDb().ConfigureAwait(false);
			PersistentData.CloseMainDb();
			await PersistentData.CloseTileCacheAsync().ConfigureAwait(false);

			MyRuntimeData?.Dispose();

			Logger.Add_TPL("CloseAll() ended", Logger.ForegroundLogFilename, Logger.Severity.Info);
		}
		private bool IsRootFrameAvailable
		{
			get
			{
				return Window.Current != null
					&& Window.Current.Content != null
					&& Window.Current.Content is Frame;
			}
		}
		private bool IsRootFrameMain
		{
			get
			{
				return IsRootFrameAvailable
					&& (Window.Current.Content as Frame).Content != null
					&& (Window.Current.Content as Frame).Content is Main;
			}
		}
		private Frame GetCreateRootFrame(IActivatedEventArgs e) //(LaunchActivatedEventArgs e) was
		{
			Frame rootFrame = null;
			if (IsRootFrameAvailable)
			{
				rootFrame = Window.Current.Content as Frame;
				rootFrame.Name = "RootFrame";
			}

			if (rootFrame == null)  // Do not repeat app initialization when the Window already has content, just ensure that the window is active
			{
				Logger.Add_TPL("Creating root frame", Logger.ForegroundLogFilename, Logger.Severity.Info);
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame() { UseLayoutRounding = true };
				rootFrame.Name = "RootFrame";

				// Set the default language
				//rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
				rootFrame.Language = Windows.Globalization.Language.CurrentInputMethodLanguageTag; //this is important and decides for the whole app

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}
			return rootFrame;
		}
		
		private void NavigateToRootFrameContent(Frame rootFrame)
		{
			if (rootFrame?.Content == null)
			{
				// Logger.Add_TPL("rootFrame.Content == null, about to navigate to Main", Logger.ForegroundLogFilename);
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter (in theory, but no parameter here, we simply navigate)
				if (!rootFrame.Navigate(typeof(Main)))
				{
					Logger.Add_TPL("Failed to create initial page", Logger.ForegroundLogFilename);
					throw new Exception("Failed to create initial page");
				}
			}
		}
		public static void ShortVibration()
		{



			VibrationDevice myDevice = VibrationDevice.GetDefault();
			myDevice.Vibrate(TimeSpan.FromSeconds(.12));
		}

		#endregion services
	}

	/// <summary>
	/// Implement this interface if your page invokes the file open picker
	/// API.
	/// </summary>
	interface IFileActivatable
	{
		/// <summary>
		/// This method is invoked when the app is opened via a file association
		/// files
		/// </summary>
		/// <param name="args">Activated event args object that contains returned files from file open </param>
		Task<List<PersistentData.Tables>> LoadFileIntoDbAsync(FileActivatedEventArgs args);
	}

	/// <summary>
	/// Implement this interface if your page invokes the file open picker
	/// API.
	/// </summary>
	interface IFileOpenPickerContinuable
	{
		/// <summary>
		/// This method is invoked when the file open picker returns picked
		/// files
		/// </summary>
		/// <param name="args">Activated event args object that contains returned files from file open picker</param>
		Task ContinueFileOpenPickerAsync(FileOpenPickerContinuationEventArgs args);
	}

	/// <summary>
	/// Implement this interface if your page invokes the file save picker
	/// API
	/// </summary>
	interface IFileSavePickerContinuable
	{
		/// <summary>
		/// This method is invoked when the file save picker returns saved
		/// files
		/// </summary>
		/// <param name="args">Activated event args object that contains returned file from file save picker</param>
		Task ContinueFileSavePickerAsync(FileSavePickerContinuationEventArgs args);
	}
}
