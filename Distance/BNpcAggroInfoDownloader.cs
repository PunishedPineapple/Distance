using System;
using System.Net.Http;
using System.Threading.Tasks;

using CheapLoc;

namespace Distance;

internal static class BNpcAggroInfoDownloader
{
	internal static async Task<BNpcAggroInfoFile> DownloadUpdatedAggroDataAsync( string filePath, UInt64 localVersionOverride = 0 )
	{
		//	Don't do anything if we're already running the task.
		if( CurrentDownloadStatus == DownloadStatus.Downloading ) return null;

		BNpcAggroInfoFile downloadedDataFile = new();

		//string url = "https://punishedpineapple.github.io/DalamudPlugins/Distance/Support/AggroDistances.dat";
		string url = "https://raw.githubusercontent.com/PunishedPineapple/PunishedPineapple.github.io/master/DalamudPlugins/Distance/Support/AggroDistances.dat";
		await Task.Run( async () =>
		{
			DownloadStatus status = DownloadStatus.Downloading;
			CurrentDownloadStatus = status;
			try
			{
				string responseBody = await LocalHttpClient.GetStringAsync( url );

				if( downloadedDataFile.ReadFromString( responseBody ) )
				{
					Service.PluginLog.Information( $"Downloaded BNpc aggro range data version {downloadedDataFile.GetFileVersionAsString()} ({downloadedDataFile.FileVersion})" );
					if( downloadedDataFile.FileVersion > ( localVersionOverride > 0 ? localVersionOverride : BNpcAggroInfo.GetCurrentFileVersion() ) )
					{
						status = DownloadStatus.FailedFileWrite;
						downloadedDataFile.WriteFile( filePath );
						Service.PluginLog.Information( $"Wrote BNpc aggro range data to disk: Version {downloadedDataFile.GetFileVersionAsString()} ({downloadedDataFile.FileVersion})" );
						status = DownloadStatus.Completed;
					}
					else
					{
						Service.PluginLog.Information( $"Downloaded file not newer than existing data file; discarding." );
						status = DownloadStatus.OutOfDateFile;
						downloadedDataFile = null;
					}
				}
				else
				{
					Service.PluginLog.Warning( $"Unable to load downloaded file!" );
					status = DownloadStatus.FailedFileLoad;
					downloadedDataFile = null;
				}
			}
			catch( HttpRequestException e )
			{
				Service.PluginLog.Warning( $"Exception occurred while trying to update aggro distance data: {e}" );
				status = DownloadStatus.FailedDownload;
				downloadedDataFile = null;
			}
			catch( TaskCanceledException )
			{
				Service.PluginLog.Information( "Aggro distance data update http request was canceled." );
				status = DownloadStatus.Canceled;
				downloadedDataFile = null;
			}
			catch( Exception e )
			{
				Service.PluginLog.Warning( $"Unknown exception occurred while trying to update aggro distance data: {e}" );
				status = DownloadStatus.FailedDownload;
				downloadedDataFile = null;
			}
			finally
			{
				CurrentDownloadStatus = status;
			}
		} );

		return downloadedDataFile;
	}

	internal static string GetDownloadStatusMessage( DownloadStatus status )
	{
		return status switch
		{
			DownloadStatus.None				=> Loc.Localize( "Download Status Message: None", "Ready" ),
			DownloadStatus.Downloading		=> Loc.Localize( "Download Status Message: Downloading", "Downloading..." ),
			DownloadStatus.FailedDownload	=> Loc.Localize( "Download Status Message: Failed Download", "Download failed!" ),
			DownloadStatus.FailedFileLoad	=> Loc.Localize( "Download Status Message: Failed File Load", "The downloaded file was invalid!" ),
			DownloadStatus.FailedFileWrite	=> Loc.Localize( "Download Status Message: Failed File Write", "The downloaded file could not be saved to disk; any updates will be lost upon reloading." ),
			DownloadStatus.OutOfDateFile	=> Loc.Localize( "Download Status Message: Out of Date File", "The downloaded file was not newer than the current data, and has been discarded." ),
			DownloadStatus.Completed		=> Loc.Localize( "Download Status Message: Completed", "Update Completed" ),
			DownloadStatus.Canceled			=> Loc.Localize( "Download Status Message: Canceled", "The update operation was canceled!" ),
			_ => "You shouldn't ever see this!",
		};
	}

	internal static string GetCurrentDownloadStatusMessage()
	{
		return GetDownloadStatusMessage( CurrentDownloadStatus );
	}

	internal static void TryResetStatusMessage()
	{
		if( CurrentDownloadStatus != DownloadStatus.Downloading )
		{
			CurrentDownloadStatus = DownloadStatus.None;
		}
	}

	internal static void CancelAllDownloads()
	{
		LocalHttpClient.CancelPendingRequests();
	}

	private static readonly HttpClient LocalHttpClient = new();

	internal static DownloadStatus CurrentDownloadStatus { get; private set; } = DownloadStatus.None;

	internal enum DownloadStatus
	{
		None,
		Downloading,
		FailedDownload,
		FailedFileLoad,
		FailedFileWrite,
		OutOfDateFile,
		Completed,
		Canceled
	}
}
