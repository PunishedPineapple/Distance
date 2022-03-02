using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

using Dalamud.Data;
using Dalamud.Logging;
using CheapLoc;

namespace Distance
{
	internal static class BNpcAggroInfoDownloader
	{
		public static void DownloadUpdatedAggroData( DataManager dataManager, string filePath )
		{
			//	Don't do anything if we're already running the task.
			if( CurrentDownloadStatus == DownloadStatus.Downloading ) return;

			string url = "https://punishedpineapple.github.io/DalamudPlugins/Distance/Support/AggroDistances.dat";
			Task.Run( async () =>
			{
				DownloadStatus status = DownloadStatus.Downloading;
				CurrentDownloadStatus = status;
				try
				{
					string responseBody = await LocalHttpClient.GetStringAsync( url );   //***** TODO: Should probably have a cancellation token set up for when disposing. *****

					BNpcAggroInfoFile downloadedDataFile = new();
					if( downloadedDataFile.ReadFromString( responseBody ) )
					{
						if( downloadedDataFile.FileVersion > BNpcAggroInfo.GetCurrentFileVersion() )
						{
							BNpcAggroInfo.Init( dataManager, downloadedDataFile );
							status = DownloadStatus.FailedFileWrite;
							downloadedDataFile.WriteFile( filePath );
							status = DownloadStatus.Completed;
						}
						else
						{
							status = DownloadStatus.OutOfDateFile;
						}
					}
					else
					{
						status = DownloadStatus.FailedFileLoad;
					}
				}
				catch( HttpRequestException e )
				{
					PluginLog.LogWarning( $"Exception occurred while trying to update aggro distance data: {e}" );
					status = DownloadStatus.FailedDownload;
				}
				catch( TaskCanceledException )
				{
					PluginLog.LogInformation( "Aggro distance data update http request was canceled." );
					status = DownloadStatus.Canceled;
				}
				finally
				{
					CurrentDownloadStatus = status;
				}
			} );
		}

		public static string GetDownloadStatusMessage( DownloadStatus status )
		{
			string str = "You shouldn't ever see this!";

			switch( status )
			{
				case DownloadStatus.None:
					str = Loc.Localize( "Download Status Message: None", "Ready" );
					break;

				case DownloadStatus.Downloading:
					str = Loc.Localize( "Download Status Message: Downloading", "Downloading..." );
					break;

				case DownloadStatus.FailedDownload:
					str = Loc.Localize( "Download Status Message: Failed Download", "Download failed!" );
					break;

				case DownloadStatus.FailedFileLoad:
					str = Loc.Localize( "Download Status Message: Failed File Load", "The downloaded file was invalid!" );
					break;

				case DownloadStatus.FailedFileWrite:
					str = Loc.Localize( "Download Status Message: Failed File Write", "The downloaded file could not be saved to disk; any updates will be lost upon reloading." );
					break;

				case DownloadStatus.OutOfDateFile:
					str = Loc.Localize( "Download Status Message: Out of Date File", "The downloaded file was older than the current data, and has been discarded." );
					break;

				case DownloadStatus.Completed:
					str = Loc.Localize( "Download Status Message: Completed", "Update Completed" );
					break;

				case DownloadStatus.Canceled:
					str = Loc.Localize( "Download Status Message: Canceled", "The update operation was canceled!" );
					break;

				default:
					str = "You shouldn't ever see this!";
					break;
			}

			return str;
		}

		public static string GetCurrentDownloadStatusMessage()
		{
			return GetDownloadStatusMessage( CurrentDownloadStatus );
		}

		public static void TryResetStatusMessage()
		{
			if( CurrentDownloadStatus != DownloadStatus.Downloading )
			{
				CurrentDownloadStatus = DownloadStatus.None;
			}
		}

		public static void CancelAllDownloads()
		{
			LocalHttpClient.CancelPendingRequests();
		}

		//	Ya we're only supposed to have one httpclient instance per program, but I don't see how that's possible for a plugin.
		private static readonly HttpClient LocalHttpClient= new();
		public static DownloadStatus CurrentDownloadStatus { get; private set; } = DownloadStatus.None;
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
}
