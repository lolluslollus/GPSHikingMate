using LolloGPS.Data;
using LolloGPS.Data.Constants;
using LolloGPS.Data.Runtime;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Utilz;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;

namespace LolloGPS.Core
{
	internal class Licenser
	{

		private static RuntimeData _runtimeData = RuntimeData.GetInstance();
		public static async Task<bool> CheckLicensedAsync()
		{
			// do not use persistent data across this class
			// coz you cannot be sure it has been read out yet.
			try
			{
#if NOSTORE && !TRIALTESTING
                return true;
#endif
				LicenseInformation licenseInformation = await GetLicenseInformation();
				if (licenseInformation.IsActive)
				{
					if (licenseInformation.IsTrial)
					{
						_runtimeData.IsTrial = true;
						bool isCheating = false;

						var installDate = await GetInstallDateAsync();
						if (LicenserData.LastInstallDate.UtcDateTime == default(DateTimeOffset))
						{
							LicenserData.LastInstallDate = installDate;
						}
						if (!LicenserData.IsDatesEqual(LicenserData.LastInstallDate, installDate))
						{
							isCheating = true;
							Logger.Add_TPL(
								"CheckLicensedAsync() found a cheat because LastInstallDate = " + LicenserData.LastInstallDate
								+ " and installDate = " + installDate,
								Logger.ForegroundLogFilename);
						}

						int usageDays = (DateTimeOffset.UtcNow - installDate.UtcDateTime).Days;
						if (usageDays >= 0)
						{
							if (usageDays >= LicenserData.LastNonNegativeUsageDays)
							{
								usageDays = Math.Max(usageDays, LicenserData.LastNonNegativeUsageDays);
								LicenserData.LastNonNegativeUsageDays = usageDays;
							}
							else
							{
								isCheating = true;
								Logger.Add_TPL(
									"CheckLicensedAsync() found a cheat because usageDays = " + usageDays
									+ " and installDate = " + installDate
									+ " and _runtimeData.LastNonNegativeUsageDays = " + LicenserData.LastNonNegativeUsageDays,
									Logger.ForegroundLogFilename);
							}
						}
						else
						{
							isCheating = true;
							Logger.Add_TPL("CheckLicensedAsync() found a cheat because usageDays = " + usageDays
								+ " and installDate = " + installDate,
								Logger.ForegroundLogFilename);
						}

						if (isCheating)
						{
							_runtimeData.TrialResidualDays = -1;
						}
						else
						{
							_runtimeData.TrialResidualDays = ConstantData.TRIAL_LENGTH_DAYS - usageDays;
						}

						if (isCheating || usageDays > ConstantData.TRIAL_LENGTH_DAYS)
						{
							return await AskQuitOrBuyAsync("This trial version has expired", "Trial expired");
						}
					}
					else
					{
						_runtimeData.IsTrial = false;
					}
				}
				else
				{
					_runtimeData.IsTrial = true;
					return await AskQuitOrBuyAsync("No licenses found for this app", "No licenses");
				}
				return true;
			}
			catch (Exception ex)
			{
				_runtimeData.IsTrial = true;
				Logger.Add_TPL("ERROR: CheckLicensedAsync() threw " + ex.ToString(), Logger.ForegroundLogFilename);
				return await AskQuitOrBuyAsync("No licenses found for this app", "No licenses");
			}
		}
		private async static Task<LicenseInformation> GetLicenseInformation()
		{
#if DEBUG && TRIALTESTING
            StorageFolder proxyDataFolder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            StorageFile proxyFile = await proxyDataFolder.GetFileAsync("WindowsStoreProxy.xml");
            await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile).AsTask();

            LicenseInformation licenseInformation = CurrentAppSimulator.LicenseInformation;
#else
			LicenseInformation licenseInformation = CurrentApp.LicenseInformation;
#endif
			return licenseInformation;
		}
		private static async Task<DateTimeOffset> GetInstallDateAsync()
		{
			// Package.Current.InstallDate is not implemented
			StorageFolder installLocationFolder = Package.Current.InstalledLocation;
			// Package.Current.InstalledLocation always has a very low install date, like 400 years ago...
			// but its descendants don't, so we take one that is bound to be there, such as Assets.
			var assetsFolder = await installLocationFolder.GetFolderAsync("Assets").AsTask<StorageFolder>().ConfigureAwait(false);
			return assetsFolder.DateCreated;
		}
		private static async Task NotifyAsync(string message1, string message2)
		{
			var dialog = new MessageDialog(message1, message2);
			UICommand okCommand = new UICommand("Ok", (command) => { });
			dialog.Commands.Add(okCommand);
			await dialog.ShowAsync().AsTask();
		}
		private static async Task<bool> AskQuitOrBuyAsync(string message1, string message2)
		{
			var dialog = new MessageDialog(message1, message2);
			UICommand quitCommand = new UICommand("Quit", (command) => { });
			dialog.Commands.Add(quitCommand);
			UICommand buyCommand = new UICommand("Buy", (command) => { });
			dialog.Commands.Add(buyCommand);
			// Set the command that will be invoked by default
			dialog.DefaultCommandIndex = 1;
			// Show the message dialog
			IUICommand reply = await dialog.ShowAsync().AsTask();

			bool isAlreadyBought = false;
			if (reply == buyCommand)
			{
				isAlreadyBought = await BuyAsync();
			}
			if (isAlreadyBought)
			{
				//_runtimeData.IsTrial = false; // LOLLO this would be better but risky. I should never arrive here anyway. What then?
				//_runtimeData.TrialResidualDays = 0; // idem
				return true;
			}
			else
			{
				await (App.Current as App).Quit();
				return false;
			}
		}
		/// <summary>
		/// Opens the store to buy the app and returns false if the app must quit.
		/// </summary>
		/// <returns></returns>
		public static async Task<bool> BuyAsync()
		{
			LicenseInformation licenseInformation = await GetLicenseInformation();
			if (licenseInformation.IsTrial)
			{
				try
				{
					// go to the store and quit instead of calling RequestAppPurchaseAsync, which is not supported
					// https://msdn.microsoft.com/en-us/library/windows/apps/mt228343.aspx
					var uri = new Uri(ConstantData.BUY_URI);

					Debug.WriteLine("Store uri = " + uri.ToString());
					await Launcher.LaunchUriAsync(uri).AsTask();
					return false; // must quit and restart to verify the purchase

					////string receipt = await CurrentAppSimulator.RequestAppPurchaseAsync(true).AsTask<String>();
					//string receipt = await CurrentApp.RequestAppPurchaseAsync(true).AsTask<String>();
					//XElement receiptXml = XElement.Parse(receipt);
					//var appReceipt = receiptXml.Element("AppReceipt");
					//if (appReceipt.Attribute("LicenseType").Value == "Full" && !licenseInformation.IsTrial && licenseInformation.IsActive)
					//{
					//    await NotifyAsync("Your purchase was successful.", "Done");
					//    return true;
					//}
					//else
					//{
					//    await NotifyAsync("You still have a trial license for this app.", "Sorry");
					//    return false;
					//}
				}
				catch (Exception)
				{
					await NotifyAsync("The upgrade transaction failed. You still have a trial license for this app.", "Sorry");
					return false;
				}
			}
			else
			{
				Logger.Add_TPL("ERROR: Licenser.BuyAsync() was called after the product had been bought already", Logger.ForegroundLogFilename);
				await NotifyAsync("You already bought this app and have a fully-licensed version.", "Done");
				return true;
			}
		}
		/// <summary>
		/// Opens the store to rate the app. Returns true if the operation succeeded.
		/// </summary>
		/// <returns></returns>
		public static async Task<bool> RateAsync()
		{
			try
			{
				var uri = new Uri(ConstantData.RATE_URI);
				await Launcher.LaunchUriAsync(uri).AsTask();
				return true;
			}
			catch (Exception)
			{
				await NotifyAsync("Could not open the store", "Sorry");
				return false;
			}
		}

		private class LicenserData
		{
			public const int LAST_NON_NEGATIVE_USAGE_DAYS_DEFAULT = 0;
			public static readonly DateTimeOffset DATE_DEFAULT = default(DateTimeOffset);

			private static int _lastNonNegativeUsageDays = LAST_NON_NEGATIVE_USAGE_DAYS_DEFAULT;
			public static int LastNonNegativeUsageDays
			{
				get { return _lastNonNegativeUsageDays; }
				set
				{
					if (_lastNonNegativeUsageDays != value)
					{
						_lastNonNegativeUsageDays = value;
						SaveLastNonNegativeUsageDays();
					}
				}
			}

			private static DateTimeOffset _lastInstallDate = default(DateTimeOffset);
			public static DateTimeOffset LastInstallDate
			{
				get { return _lastInstallDate; }
				set
				{
					if (_lastInstallDate != value)
					{
						_lastInstallDate = value;
						SaveLastInstallDate();
					}
				}
			}
			static LicenserData()
			{
				string lastNonNegativeUsageDaysString = RegistryAccess.GetValue(nameof(LastNonNegativeUsageDays));
				try
				{
					_lastNonNegativeUsageDays = Convert.ToInt32(lastNonNegativeUsageDaysString, CultureInfo.InvariantCulture);
				}
				catch (Exception)
				{
					_lastNonNegativeUsageDays = LAST_NON_NEGATIVE_USAGE_DAYS_DEFAULT;
				}

				_lastInstallDate = LoadLastInstallDate();
			}

			private static void SaveLastNonNegativeUsageDays()
			{
				string lastNonNegativeUsageDaysString = _lastNonNegativeUsageDays.ToString(CultureInfo.InvariantCulture);
				RegistryAccess.SetValue(nameof(LastNonNegativeUsageDays), lastNonNegativeUsageDaysString);
			}
			private static DateTimeOffset LoadLastInstallDate()
			{
				string lastInstallDate = RegistryAccess.GetValue(nameof(LastInstallDate));
				long ticks = default(long);
				if (long.TryParse(lastInstallDate, out ticks))
				{
					return DateTimeOffset.FromFileTime(ticks);
				}
				else
				{
					return DATE_DEFAULT;
				}
			}
			private static void SaveLastInstallDate()
			{
				string lastInstallDate = _lastInstallDate.ToFileTime().ToString(CultureInfo.InvariantCulture);
				RegistryAccess.SetValue(nameof(LastInstallDate), lastInstallDate);
			}
			public static bool IsDatesEqual(DateTimeOffset one, DateTimeOffset two)
			{
				return one.UtcDateTime.ToString(ConstantData.DATE_TIME_FORMAT, CultureInfo.InvariantCulture).Equals(two.UtcDateTime.ToString(ConstantData.DATE_TIME_FORMAT, CultureInfo.InvariantCulture));
			}
		}
	}
	public static class DateTimeOffsetExtensions
	{
		public static bool IsBefore(this DateTimeOffset me, DateTimeOffset dtoToCompare)
		{
			return me.UtcTicks < dtoToCompare.UtcTicks;
		}
		public static bool IsAfter(this DateTimeOffset me, DateTimeOffset dtoToCompare)
		{
			return me.UtcTicks > dtoToCompare.UtcTicks;
		}
	}
}
