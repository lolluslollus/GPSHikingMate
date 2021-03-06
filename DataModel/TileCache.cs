﻿using LolloGPS.Data.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Utilz;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace LolloGPS.Data.TileCache
{
	// public enum TileSources { Nokia, OpenStreetMap, Swisstopo, Wanderreitkarte, OrdnanceSurvey, ForUMaps, OpenSeaMap, UTTopoLight, ArcGIS }

	public sealed class TileCache // : IDisposable
	{
		public const string MimeTypeImageAny = "image/*"; // "image/png"
														  //public const string ImageToCheck = "image";
		public const int MaxRecords = 65535;

		private readonly TileSourceRecord _tileSource = TileSourceRecord.GetDefaultTileSource(); //TileSources.Nokia;
																								 /// <summary>
																								 /// The tile source will give its name to the file folder
																								 /// </summary>
		public TileSourceRecord TileSource { get { return _tileSource; } }

		private StorageFolder _imageFolder = null;
		/// <summary>
		/// The image folder has exactly the same name as the tile source
		/// </summary>
		public StorageFolder ImageFolder { get { return _imageFolder; } }

		private bool _isCaching = false;
		/// <summary>
		/// Gets if this cache writes away (ie caches) the data it picks up.
		/// Only relevant for supplying map tiles on the fly.
		/// We could read this from PersistentData whenever we need it, but it does not work well.
		/// </summary>
		public bool IsCaching { get { return _isCaching; } }

		private readonly string _webUriFormat = string.Empty;
		private const string _tileFileFormat = "{3}_{0}_{1}_{2}";

		#region construct and dispose
		public TileCache(TileSourceRecord tileSource, bool isCaching)
		{
			if (tileSource == null) throw new ArgumentNullException("TileCache ctor was given tileSource == null");
			TileSourceRecord.Clone(tileSource, ref _tileSource);
			try
			{
				_webUriFormat = _tileSource.UriString.Replace(TileSourceRecord.ZoomLevelPlaceholder, TileSourceRecord.ZoomLevelPlaceholder_Internal);
				_webUriFormat = _webUriFormat.Replace(TileSourceRecord.XPlaceholder, TileSourceRecord.XPlaceholder_Internal);
				_webUriFormat = _webUriFormat.Replace(TileSourceRecord.YPlaceholder, TileSourceRecord.YPlaceholder_Internal);
			}
			catch (Exception exc)
			{
				Debug.WriteLine("Exception in TileCache.ctor: " + exc.Message + exc.StackTrace);
			}

			_isCaching = isCaching;

			//if (_tileSource.IsTesting)
			//{
			//    _imageFolder = null;
			//}
			//else if (_imageFolder == null)
			//{
			_imageFolder = ApplicationData.Current.LocalFolder.CreateFolderAsync(_tileSource.TechName, CreationCollisionOption.OpenIfExists).AsTask().Result;
			//}
		}
		//public void Dispose()
		//{
		//    ProcessingQueue.Dispose();
		//}
		// activate and deactivate are taken care of in PersistentData
		#endregion construct and dispose

		#region getters
		public Uri GetUriForFile(string fileName)
		{
			return new UriBuilder(System.IO.Path.Combine(_imageFolder.Path, fileName)).Uri;
		}
		/// <summary>
		/// gets the web uri of the tile (TileSource, X, Y, Z and Zoom)
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public string GetWebUri(int x, int y, int z, int zoom)
		{
			try
			{
				return string.Format(_webUriFormat, zoom, x, y);
			}
			catch (Exception exc)
			{
				Debug.WriteLine("Exception in TileCache.GetWebUri(): " + exc.Message + exc.StackTrace);
				return string.Empty;
			}
		}
		/// <summary>
		/// gets the filename that uniquely identifies a tile (TileSource, X, Y, Z and Zoom)
		/// ProcessingQueue is based on a list of strings, which are nothing else than the file names,
		/// so every different tile source must produce a different file name, 
		/// even if X, Y, Z and Zoom are equal.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public string GetFileNameFromKey(int x, int y, int z, int zoom)
		{
			return string.Format(_tileFileFormat, zoom, x, y, _tileSource.TechName); // was
																					 // return string.Format(_tileFileFormat, zoom, x, y, _tileSource.TechName) + ".jpg"; // test (useless)
		}
		//public abstract string GetWebUriFormat();
		public int GetTilePixelSize()
		{
			return _tileSource.TilePixelSize;
		}
		public int GetMinZoom()
		{
			return _tileSource.MinZoom;
		}
		public int GetMaxZoom()
		{
			return _tileSource.MaxZoom;
		}
		//public bool GetIsTesting()
		//{
		//    return _tileSource.IsTesting;
		//}
		//public bool GetIsDefault()
		//{
		//    return _tileSource.IsDefault;
		//}
		#endregion getters

		//public async Task<RandomAccessStreamReference> GetTileStreamRef(int x, int y, int z, int zoom)
		//{
		//	// out of range? get out, no more thoughts
		//	if (zoom < GetMinZoom() || zoom > GetMaxZoom()) return null;
		//	// get the filename that uniquely identifies TileSource, X, Y, Z and Zoom
		//	string fileName = GetFileNameFromKey(x, y, z, zoom);
		//	// not working on this set of data? Mark it as busy, closing the gate for other threads
		//	// already working on this set of data? Don't duplicate web requests of file accesses or any extra work and return null
		//	if (!await ProcessingQueue.TryAddToQueueAsync(fileName).ConfigureAwait(false)) return null;
		//	// from now on, any returns must be preceded by removing the current fileName from the processing queue, to reopen the gate!
		//	RandomAccessStreamReference output = null;
		//	int where = 0;
		//	string sWebUri = GetWebUri(x, y, z, zoom);
		//	// check if I have this tile in the cache already
		//	String fileNameFromDb = await TileCacheRecord.GetFilenameFromDbAsync(_tileSource, x, y, z, zoom).ConfigureAwait(false);
		//	try
		//	{
		//		// tile is not in cache: 
		//		// download it, save it and return an uri pointing at it; 
		//		if (string.IsNullOrWhiteSpace(fileNameFromDb))
		//		{
		//			where = 1;
		//			// download the tile, save it and return an uri pointing at it (ie at its file)
		//			if (RuntimeData.GetInstance().IsConnectionAvailable)
		//			{
		//				var request = WebRequest.CreateHttp(sWebUri); request.Accept = MimeTypeImageAny;
		//				where = 2;
		//				using (var response = await request.GetResponseAsync().ConfigureAwait(false))
		//				{
		//					if (IsWebResponseHeaderOk(response))
		//					{
		//						where = 3;
		//						using (var readStream = response.GetResponseStream()) // note that I cannot read the length of this stream, nor change its position
		//						{
		//							where = 4;
		//							// read response stream into a new record. 
		//							// This extra step is the price to pay if we want to check the stream content
		//							TileCacheRecord newRecord = new TileCacheRecord(_tileSource.TechName, x, y, z, zoom) { FileName = fileName, Img = new byte[response.ContentLength] };
		//							await readStream.ReadAsync(newRecord.Img, 0, (int)response.ContentLength).ConfigureAwait(false);

		//							// using (var writeStream = await _imageFolder.OpenStreamForWriteAsync(fileName, CreationCollisionOption.OpenIfExists).ConfigureAwait(false))
		//							// check readStream: at least one byte must not be empty
		//							if (IsWebResponseContentOk(newRecord))
		//							{
		//								StorageFile newFile = await _imageFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);
		//								using (var writeStream = await newFile.OpenStreamForWriteAsync().ConfigureAwait(false))
		//								{
		//									if (writeStream.Length > 0) // file already exists, do not overwrite it - extra caution, it should never happen
		//									{
		//										where = 99;
		//										// String testFileName = await TileCacheRecord.GetFilenameFromDbAsync(_tileSource, x, y, z, zoom).ConfigureAwait(false);
		//										// Debug.WriteLine("GetTileUri() attempted to overwrite existing file " + fileName);
		//										output = await TileCacheRecord.GetPixelStreamRefFromByteArray(newRecord.Img).ConfigureAwait(false);
		//									}
		//									else
		//									{
		//										where = 7;
		//										await writeStream.WriteAsync(newRecord.Img, 0, newRecord.Img.Length).ConfigureAwait(false); // I cannot use readStream.CopyToAsync() coz, after reading readStream, its cursor has advanced and we cannot turn it back
		//										where = 8;
		//										writeStream.Flush();
		//										// check file vs stream
		//										var fileProps = await newFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
		//										var fileSize = fileProps.Size;
		//										where = 9;
		//										if ((long)fileSize == writeStream.Length && writeStream.Length > 0)
		//										{
		//											where = 10;
		//											bool isInserted = await DBManager.TryInsertIntoTileCacheAsync(newRecord, false).ConfigureAwait(false);
		//											output = await TileCacheRecord.GetPixelStreamRefFromByteArray(newRecord.Img).ConfigureAwait(false);
		//										}
		//									}
		//								}
		//							}
		//						}
		//					}
		//				}
		//			}
		//		}
		//		// tile is in cache: return an uri pointing at it (ie at its file)
		//		else
		//		{
		//			output = await TileCacheRecord.GetPixelStreamRefFromImgFolder(_imageFolder, fileName).ConfigureAwait(false);
		//			if (fileName != fileNameFromDb)
		//			{
		//				Task update = UpdateFileNameAsync(fileNameFromDb, fileName, _tileSource.TechName, x, y, z, zoom);
		//			}
		//		}
		//	}
		//	catch (Exception exc0)
		//	{
		//		Debug.WriteLine("ERROR in GetTileStreamRef(): " + exc0.Message + " ; I made it to where = " + where + exc0.StackTrace);
		//	}
		//	await ProcessingQueue.RemoveFromQueueAsync(fileName).ConfigureAwait(false);
		//	return output;
		//}
		public static async Task<int> TryClearAsync(TileSourceRecord tileSource)
		{
			Debug.WriteLine("About to call ProcessingQueue.ClearCacheIfQueueEmptyAsync");
			int howManyRecordsDeleted = await ProcessingQueue.ClearCacheIfQueueEmptyAsync(tileSource).ConfigureAwait(false);
			Debug.WriteLine("returned from ProcessingQueue.ClearCacheIfQueueEmptyAsync");
			return howManyRecordsDeleted;
		}

		private static async Task<int> ClearAsync(TileSourceRecord tileSource)
		{
			Debug.WriteLine("ClearAsync() started");
			int howManyRecordsDeletedTotal = -5;

			//// test begin
			//await GetAllFilesInLocalFolder().ConfigureAwait(false);
			//// test end

			string[] folderNamesToBeDeleted = GetFolderNamesToBeDeleted(tileSource);
			if (folderNamesToBeDeleted != null && folderNamesToBeDeleted.Length > 0)
			{
				StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

				foreach (var folderName in folderNamesToBeDeleted)
				{
					if (!string.IsNullOrWhiteSpace(folderName))
					{
						try
						{
							// delete db entries first.
							/*
							 *  It's not terrible if some files are not deleted and the db thinks they are:
								they will be downloaded again, and not resaved (with the current logic).
							 *  It's terrible if files are deleted and the db thinks they are still there,
								because they will never be downloaded again, and the tiles will be forever empty.
							 */
							int howManyRecordsDeletedNow = await DBManager.DeleteTileCacheAsync(folderName).ConfigureAwait(false); // important when suspending!
							if (howManyRecordsDeletedNow > 0)
							{
								if (howManyRecordsDeletedTotal < 0) howManyRecordsDeletedTotal = 0;
								howManyRecordsDeletedTotal += howManyRecordsDeletedNow;
								// delete the files next.
								StorageFolder imageFolder = await localFolder.GetFolderAsync(folderName).AsTask().ConfigureAwait(false);
								await imageFolder.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().ConfigureAwait(false);
							}
							else if (howManyRecordsDeletedNow == 0)
							{
								if (howManyRecordsDeletedTotal < 0) howManyRecordsDeletedTotal = 0;
							}
							else if (howManyRecordsDeletedNow < 0) // db closed or error in db
							{
								howManyRecordsDeletedTotal = howManyRecordsDeletedNow;
								break;
							}
						}
						catch (FileNotFoundException)
						{
							Debug.WriteLine("FileNotFound in TryClearAsync()");
						}
						catch (Exception ex)
						{
							Debug.WriteLine("ERROR in TryClearAsync: " + ex.Message + ex.StackTrace);
						}
					}
				}
			}
			Debug.WriteLine("ClearAsync() ended");
			return howManyRecordsDeletedTotal;
		}

		private static string[] GetFolderNamesToBeDeleted(TileSourceRecord tileSource)
		{
			string[] folderNamesToBeDeleted = null;
			PersistentData _persistentData = PersistentData.GetInstance();
			if (!tileSource.IsAll && !tileSource.IsNone)
			{
				folderNamesToBeDeleted = new string[1] { tileSource.TechName };
			}
			else if (tileSource.IsAll)
			{
				folderNamesToBeDeleted = new string[_persistentData.TileSourcez.Count - 1];
				int cnt = 0;
				foreach (var item in _persistentData.TileSourcez)
				{
					if (!item.IsDefault)
					{
						folderNamesToBeDeleted[cnt] = item.TechName;
						cnt++;
					}
				}
			}
			return folderNamesToBeDeleted;
		}
		public async Task<Uri> GetTileUri(int x, int y, int z, int zoom)
		{
			//bool isOnUIThread = CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess;
			//if (isOnUIThread) Debug.WriteLine("GetTileUri() is running on the UI thread!");
			// out of range? get out, no more thoughts
			if (zoom < GetMinZoom() || zoom > GetMaxZoom()) return null;
			// get the filename that uniquely identifies TileSource, X, Y, Z and Zoom
			string fileName = GetFileNameFromKey(x, y, z, zoom);
			// not working on this set of data? Mark it as busy, closing the gate for other threads
			// already working on this set of data? Don't duplicate web requests of file accesses or any extra work and return null
			if (!await ProcessingQueue.TryAddToQueueAsync(fileName).ConfigureAwait(false)) return null;
			// from now on, any returns must happen after removing the current fileName from the processing queue, to reopen the gate!
			Uri output = null;
			int where = 0;
			string sWebUri = GetWebUri(x, y, z, zoom);
			// check if I have this tile in the cache already
			String fileNameFromDb = await TileCacheRecord.GetFilenameFromDbAsync(_tileSource, x, y, z, zoom).ConfigureAwait(false);
			//Debug.WriteLine("GetTileUri() calculated" + Environment.NewLine
			//    + "_imageFolder = " + (_imageFolder == null ? "null" : _imageFolder.Path) + Environment.NewLine
			//    + "fileName = " + fileName + Environment.NewLine
			//    + "fileNameFromDb = " + fileNameFromDb + Environment.NewLine
			//    + "sWebUri = " + sWebUri + Environment.NewLine);

			try
			{
				// tile is not in cache: 
				// if caching is active, download it, save it and return an uri pointing at it; 
				// otherwise return its web uri                
				if (string.IsNullOrWhiteSpace(fileNameFromDb))
				{
					where = 1;
					bool isConnectionAvailable = RuntimeData.GetInstance().IsConnectionAvailable;
					// tile not in cache and caching on: download the tile, save it and return an uri pointing at it (ie at its file) 
					if (IsCaching && isConnectionAvailable)
					{
						var request = WebRequest.CreateHttp(sWebUri); request.Accept = MimeTypeImageAny;
						where = 2;
						using (var response = await request.GetResponseAsync().ConfigureAwait(false))
						{
							if (IsWebResponseHeaderOk(response))
							{
								where = 3;
								using (var responseStream = response.GetResponseStream()) // note that I cannot read the length of this stream, nor change its position
								{
									where = 4;
									// read response stream into a new record. 
									// This extra step is the price to pay if we want to check the stream content
									TileCacheRecord newRecord = new TileCacheRecord(_tileSource.TechName, x, y, z, zoom) { FileName = fileName, Img = new byte[response.ContentLength] };
									await responseStream.ReadAsync(newRecord.Img, 0, (int)response.ContentLength).ConfigureAwait(false);

									// using (var writeStream = await _imageFolder.OpenStreamForWriteAsync(fileName, CreationCollisionOption.OpenIfExists).ConfigureAwait(false))
									// check readStream: at least one byte must not be empty
									if (IsWebResponseContentOk(newRecord))
									{
										StorageFile newFile = await _imageFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);
										using (var writeStream = await newFile.OpenStreamForWriteAsync().ConfigureAwait(false))
										{
											//Debug.WriteLine("GetTileUri() found: " + Environment.NewLine
											//    + "writeStream.Length = " + writeStream.Length + Environment.NewLine
											//    + "response.ContentLength = " + response.ContentLength + Environment.NewLine
											//    + "Uri4File will be = " + GetUriForFile(fileName) + Environment.NewLine);
											if (writeStream.Length > 0) // file already exists, do not overwrite it - extra caution, it should never happen 
																		// TODO check this: it may be wiser to overwrite the file. Just replace the collision option.
											{
												where = 99;
												output = GetUriForFile(fileName);
												Debug.WriteLine("GetTileUri() avoided overwriting a file with name = " + fileName + " and returned its uri = " + output.ToString());
											}
											else
											{
												where = 7;
												await writeStream.WriteAsync(newRecord.Img, 0, newRecord.Img.Length).ConfigureAwait(false); // I cannot use readStream.CopyToAsync() coz, after reading readStream, its cursor has advanced and we cannot turn it back
												where = 8;
												writeStream.Flush();
												// check file vs stream
												var fileProps = await newFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
												var fileSize = fileProps.Size;
												where = 9;
												if ((long)fileSize == writeStream.Length && writeStream.Length > 0)
												{
													where = 10;
													bool isInserted = await DBManager.TryInsertIntoTileCacheAsync(newRecord, false).ConfigureAwait(false);
													output = GetUriForFile(fileName);
													if (isInserted) where = 11;
													// Debug.WriteLine("GetTileUri() saved a file with name = " + fileName + " and returned its uri = " + output.ToString());
												}
											}
										}
									}
								}
							}
						}
					}
					// tile not in cache and cache off: return the web uri of the tile
					else
					{
						if (isConnectionAvailable) output = new Uri(sWebUri);
						//Debug.WriteLine("GetTileUri() found no files in cache and cache is off, so it only returned web uri = " + output.ToString());
					}
				}
				// tile is in cache: return an uri pointing at it (ie at its file)
				else
				{
					output = GetUriForFile(fileName);
					// Debug.WriteLine("GetTileUri() found a file with name = " + fileName + " and returned its uri = " + output.ToString());
					if (fileName != fileNameFromDb)
					{
						Task update = UpdateFileNameAsync(fileNameFromDb, fileName, _tileSource.TechName, x, y, z, zoom);
					}
				}
			}
			catch (Exception exc0)
			{
				Debug.WriteLine("ERROR in GetTileUri(): " + exc0.Message + " ; I made it to where = " + where + exc0.StackTrace);
			}
			Debug.WriteLine("GetTileUri() ended with where = " + where);
			await ProcessingQueue.RemoveFromQueueAsync(fileName).ConfigureAwait(false);
			return output;
		}
		private async Task<bool> UpdateFileNameAsync(string oldFileName, string newFileName, string techName, int x, int y, int z, int zoom)
		{
			Debug.WriteLine("ERROR in GetTileStreamRef() or GetTileUri(): file name in db is " + oldFileName + " but the calculated file name is " + newFileName);
			bool output = false;
			if (oldFileName != newFileName && _imageFolder != null)
			{
				try
				{
					StorageFile file = await _imageFolder.GetFileAsync(oldFileName).AsTask().ConfigureAwait(false);
					if (file != null)
					{
						// like when you clear: update the file first, then the db, it the file update went well.
						await file.RenameAsync(newFileName).AsTask().ConfigureAwait(false);
						TileCacheRecord tcr = new TileCacheRecord(techName, x, y, z, zoom) { FileName = newFileName };
						output = await DBManager.UpdateTileCacheRecordAsync(tcr);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Exception in UpdateFileNameAsync(): " + ex.Message + ex.StackTrace);
				}
			}
			if (output) Debug.WriteLine("The error was fixed");
			else Debug.WriteLine("The error was not fixed");
			return output;
		}
		public async Task<bool> SaveTileAsync(int x, int y, int z, int zoom) //int x, int y, int z, int zoom)
		{
			//if (GetIsTesting()) return false;
			// get the filename that uniquely identifies TileSource, X, Y, Z and Zoom
			string fileName = GetFileNameFromKey(x, y, z, zoom);
			// not working on this set of data? Mark it as busy, closing the gate for other threads
			// already working on this set of data? Don't duplicate web requests of file accesses or any extra work and return null
			if (!await ProcessingQueue.TryAddToQueueAsync(fileName).ConfigureAwait(false)) return false;
			// from now on, any returns must happen after removing the current fileName from the processing queue, to reopen the gate!
			bool output = false;
			int where = 0;
			string sWebUri = GetWebUri(x, y, z, zoom);
			// check if I have this tile in the cache already
			String fileNameFromDb = await TileCacheRecord.GetFilenameFromDbAsync(_tileSource, x, y, z, zoom).ConfigureAwait(false);
			try
			{
				// tile is not in cache: 
				// if caching is active, download it, save it and return an uri pointing at it; 
				// otherwise return its web uri                
				if (string.IsNullOrWhiteSpace(fileNameFromDb))
				{
					where = 1;
					// tile not in cache and caching on: download the tile, save it and return an uri pointing at it (ie at its file)
					if (RuntimeData.GetInstance().IsConnectionAvailable)
					{
						var request = WebRequest.CreateHttp(sWebUri); request.Accept = MimeTypeImageAny;
						where = 2;
						using (var response = await request.GetResponseAsync().ConfigureAwait(false))
						{
							if (IsWebResponseHeaderOk(response))
							{
								where = 3;
								using (var readStream = response.GetResponseStream()) // note that I cannot read the length of this stream, nor change its position
								{
									where = 4;
									// read response stream into a new record. 
									// This extra step is the price to pay if we want to check the stream content
									TileCacheRecord newRecord = new TileCacheRecord(_tileSource.TechName, x, y, z, zoom) { FileName = fileName, Img = new byte[response.ContentLength] };
									await readStream.ReadAsync(newRecord.Img, 0, (int)response.ContentLength).ConfigureAwait(false);

									// using (var writeStream = await _imageFolder.OpenStreamForWriteAsync(fileName, CreationCollisionOption.OpenIfExists).ConfigureAwait(false))
									// check readStream: at least one byte must not be empty
									if (IsWebResponseContentOk(newRecord))
									{
										StorageFile newFile = await _imageFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);
										using (var writeStream = await newFile.OpenStreamForWriteAsync().ConfigureAwait(false))
										{
											if (writeStream.Length > 0) // file already exists, do not overwrite it - extra caution, it should never happen
											{
												where = 99;
												// string testFileName = await TileCacheRecord.GetFilenameFromDbAsync(_tileSource, x, y, z, zoom).ConfigureAwait(false);
												// Debug.WriteLine("GetTileUri() attempted to overwrite existing file " + fileName);
												output = true;
												Debug.WriteLine("SaveTileAsync() avoided overwriting a file with name = " + fileName);
											}
											else
											{
												where = 7;
												await writeStream.WriteAsync(newRecord.Img, 0, newRecord.Img.Length).ConfigureAwait(false); // I cannot use readStream.CopyToAsync() coz, after reading readStream, its cursor has advanced and we cannot turn it back
												where = 8;
												writeStream.Flush();
												// check file vs stream
												var fileProps = await newFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
												var fileSize = fileProps.Size;
												where = 9;
												if ((long)fileSize == writeStream.Length && writeStream.Length > 0)
												{
													where = 10;
													bool isInserted = await DBManager.TryInsertIntoTileCacheAsync(newRecord, false).ConfigureAwait(false);
													output = true;
													Debug.WriteLine("SaveTileAsync() saved a file with name = ");
												}
											}
										}
									}
								}
							}
						}
					}
				}
				// tile is in cache: return an uri pointing at it (ie at its file)
				else
				{
					output = true;
					Debug.WriteLine("SaveTileAsync() found a file with name = " + fileName + " and returned its uri = " + output.ToString());
					if (fileName != fileNameFromDb) Debug.WriteLine("ERROR in SaveTileAsync(): file name in db is " + fileNameFromDb + " but the calculated file name is " + fileName);
				}
			}
			catch (Exception exc0)
			{
				Debug.WriteLine("ERROR in SaveTileAsync(): " + exc0.Message + " ; I made it to where = " + where + exc0.StackTrace);
			}
			Debug.WriteLine("SaveTileAsync() ended with where = " + where);
			await ProcessingQueue.RemoveFromQueueAsync(fileName).ConfigureAwait(false);

			return output;
		}

		private static bool IsWebResponseContentOk(TileCacheRecord newRecord)
		{
			int howManyBytesToCheck = 100;
			if (newRecord.Img.Length > howManyBytesToCheck)
			{
				try
				{
					for (int i = newRecord.Img.Length - 1; i > newRecord.Img.Length - howManyBytesToCheck; i--)
					{
						if (newRecord.Img[i] != 0)
						{
							return true;
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
					return false;
				}
			}
			//isStreamOk = newRecord.Img.FirstOrDefault(a => a != 0) != null; // this may take too long, so we only check the last 100 bytes
			return false;
		}
		private static bool IsWebResponseHeaderOk(WebResponse response)
		{
			return response.ContentLength > 0; //  && response.ContentType.Contains(ImageToCheck);
											   // swisstopo answers with a binary/octet-stream
		}

		/// <summary>
		/// As soon as a file (ie a unique combination of TileSource, X, Y, Z and Zoom) is in process, this class stores it.
		/// </summary>
		public sealed class ProcessingQueue
		{
			public static event PropertyChangedEventHandler PropertyChanged;
			private static void RaisePropertyChanged([CallerMemberName] String propertyName = "")
			{
				var listener = PropertyChanged;
				if (listener != null)
				{
					listener(null, new PropertyChangedEventArgs(propertyName));
				}
			}

			private static bool _isFree = true;
			/// <summary>
			/// Tells if the processing queue is free or busy at the moment.
			/// </summary>
			public static bool IsFree
			{
				get { return _isFree; }
				private set
				{
					if (_isFree != value)
					{
						_isFree = value;
						RaisePropertyChanged();
					}
				}
			}
			private static void UpdateIsFree()
			{
				IsFree = (_fileNames_InProcess.Count == 0);
			}

			private static SemaphoreSlimSafeRelease _semaphore = new SemaphoreSlimSafeRelease(1, 1);
			private static List<string> _fileNames_InProcess = new List<string>();

			/// <summary>
			/// Not working on this set of data? Mark it as busy, closing the gate for other threads.
			/// Already working on this set of data? Say so.
			/// </summary>
			/// <param name="fileName"></param>
			/// <returns></returns>
			internal static async Task<bool> TryAddToQueueAsync(string fileName)
			{
				try
				{
					await _semaphore.WaitAsync().ConfigureAwait(false);
					if (!_fileNames_InProcess.Contains(fileName))
					{
						_fileNames_InProcess.Add(fileName);
						return true;
					}
					return false;
				}
				catch (Exception) { } // semaphore disposed
				finally
				{
					UpdateIsFree();
					SemaphoreSlimSafeRelease.TryRelease(_semaphore);
				}
				return false;
			}
			internal static async Task RemoveFromQueueAsync(string fileName)
			{
				try
				{
					await _semaphore.WaitAsync().ConfigureAwait(false);
					_fileNames_InProcess.Remove(fileName);
				}
				catch (Exception) { } // semaphore disposed
				finally
				{
					UpdateIsFree();
					SemaphoreSlimSafeRelease.TryRelease(_semaphore);
				}
			}
			internal static async Task<int> ClearCacheIfQueueEmptyAsync(TileSourceRecord tileSource)
			{
				int output = -5; // if it does not go through the following, report a negative value, meaning "cache busy"
				try
				{
					await _semaphore.WaitAsync().ConfigureAwait(false);
					if (IsFree)
					{
						IsFree = false;
						try
						{
							Debug.WriteLine("About to invoke the action");
							output = await TileCache.ClearAsync(tileSource).ConfigureAwait(false);
							Debug.WriteLine("Action invoked");
						}
						catch (Exception ex)
						{
							Debug.WriteLine("ERROR in ClearCacheIfQueueEmptyAsync(); " + ex.Message + ex.StackTrace);
						}
						IsFree = true;
					}
				}
				catch (Exception) { } // semaphore disposed
				finally
				{
					SemaphoreSlimSafeRelease.TryRelease(_semaphore);
				}
				return output;
			}
		}
	}

	//public const string TileFilePathFormat = "ms-appx:///TileSourceAssets/OpenStretTest_{0}_{1}_{2}.png";
	//public const string TileFilePathFormat = "ms-appdata:///local/TileSourceAssets/OpenStretTest_{0}_{1}_{2}.png";
	//public class TileCacheOSM : TileCache
	//{
	//    private readonly string TileFileFormat = "OpenStretTest_{0}_{1}_{2}.png";
	//    private readonly string WebUriFormatInternal = "http://a.tile.openstreetmap.org/{0}/{1}/{2}.png"; // tha a after http:// can be a, b or c
	//    //private readonly string WebUriFormat = "http://a.tile.openstreetmap.org/{zoomlevel}/{x}/{y}.png"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 18;
	//    private readonly int MinZoom = 0;

	//    internal TileCacheOSM(TileSources tileSource) : base(tileSource) { }
	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        string fileName = string.Format(TileFileFormat, zoom, x, y);
	//        return fileName;
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        string sWebUri = string.Format(WebUriFormatInternal, zoom, x, y);
	//        return sWebUri;
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//public class TileCacheOpenSeaMap : TileCache
	//{
	//    private readonly string TileFileFormat = "OpenStretTest_{0}_{1}_{2}.png";
	//    private readonly string WebUriFormatInternal = "http://tiles.openseamap.org/seamark/{0}/{1}/{2}.png"; // tha a after http:// can be a, b or c
	//    //private readonly string WebUriFormat = "http://tiles.openseamap.org/seamark/{zoomlevel}/{x}/{y}.png"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 18;
	//    private readonly int MinZoom = 9;

	//    internal TileCacheOpenSeaMap(TileSources tileSource) : base(tileSource) { }
	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        string fileName = string.Format(TileFileFormat, zoom, x, y);
	//        return fileName;
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        string sWebUri = string.Format(WebUriFormatInternal, zoom, x, y);
	//        return sWebUri;
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//// to add more sources check out http://windowsmobile.navicomputer.com/blog/?p=37
	//public class TileCacheSwisstopo : TileCache
	//{
	//    private readonly string TileFileFormat = "Swisstopo_{0}_{1}_{2}.jpg";
	//    private readonly string WebUriFormatInternal = "http://mpa3.mapplus.ch/swisstopo/{0}/{1}/{2}.jpg";
	//    //private readonly string WebUriFormat = "http://mpa3.mapplus.ch/swisstopo/{zoomlevel}/{x}/{y}.jpg"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 16;
	//    private readonly int MinZoom = 7;

	//    internal TileCacheSwisstopo(TileSources tileSource) : base(tileSource) { }

	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(TileFileFormat, zoom, x, y);
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(WebUriFormatInternal, zoom, x, y);
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//public class TileCacheWanderreitkarte : TileCache
	//{
	//    private readonly string TileFileFormat = "Wanderreitkarte_{0}_{1}_{2}.png";
	//    private readonly string WebUriFormatInternal = "http://topo.wanderreitkarte.de/topo/{0}/{1}/{2}.png"; // tha a after http:// can be a, b or c
	//    //private readonly string WebUriFormat = "http://topo.wanderreitkarte.de/topo/{zoomlevel}/{x}/{y}.png"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 18;
	//    private readonly int MinZoom = 2;

	//    internal TileCacheWanderreitkarte(TileSources tileSource) : base(tileSource) { }
	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(TileFileFormat, zoom, x, y);
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(WebUriFormatInternal, zoom, x, y);
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//public class TileCacheOS : TileCache
	//{
	//    private readonly string TileFileFormat = "OS_{0}_{1}_{2}.png";
	//    private readonly string WebUriFormatInternal = "http://a.os.openstreetmap.org/sv/{0}/{1}/{2}.png"; // tha a after http:// can be a, b or c
	//    //private readonly string WebUriFormat = "http://a.os.openstreetmap.org/sv/{zoomlevel}/{x}/{y}.png"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 17;
	//    private readonly int MinZoom = 7;

	//    internal TileCacheOS(TileSources tileSource) : base(tileSource) { }
	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(TileFileFormat, zoom, x, y);
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(WebUriFormatInternal, zoom, x, y);
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//public class TileCache4UMaps : TileCache
	//{
	//    private readonly string TileFileFormat = "ForUMaps_{0}_{1}_{2}.png";
	//    private readonly string WebUriFormatInternal = "http://4umaps.eu/{0}/{1}/{2}.png"; // tha a after http:// can be a, b or c
	//    //private readonly string WebUriFormat = "http://4umaps.eu/{zoomlevel}/{x}/{y}.png"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 15;
	//    private readonly int MinZoom = 2;

	//    internal TileCache4UMaps(TileSources tileSource) : base(tileSource) { }
	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(TileFileFormat, zoom, x, y);
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(WebUriFormatInternal, zoom, x, y);
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//public class TileCacheUTTopoLight : TileCache
	//{
	//    private readonly string TileFileFormat = "TileCacheUTTopoLight_{0}_{1}_{2}.png";
	//    private readonly string WebUriFormatInternal = "http://a-kartcache.nrk.no/tiles/ut_topo_light/{0}/{1}/{2}.jpg"; // tha a after http:// can be a, b or c
	//    //private readonly string WebUriFormat = "http://a-kartcache.nrk.no/tiles/ut_topo_light/{zoomlevel}/{x}/{y}.jpg"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 16;
	//    private readonly int MinZoom = 5;

	//    internal TileCacheUTTopoLight(TileSources tileSource) : base(tileSource) { }
	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(TileFileFormat, zoom, x, y);
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(WebUriFormatInternal, zoom, x, y);
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//public class TileCacheArcGIS : TileCache
	//{
	//    private readonly string TileFileFormat = "ArcGIS_{0}_{1}_{2}.png";
	//    private readonly string WebUriFormatInternal = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{0}/{2}/{1}"; // tha a after http:// can be a, b or c
	//    //private readonly string WebUriFormat = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{zoomLevel}/{y}/{x}"; // documented at https://msdn.microsoft.com/en-us/library/windows.ui.xaml.controls.maps.httpmaptiledatasource.uriformatstring.aspx
	//    private readonly int TilePixelSize = 256;
	//    private readonly int MaxZoom = 16;
	//    private readonly int MinZoom = 0;

	//    internal TileCacheArcGIS(TileSources tileSource) : base(tileSource) { }
	//    public override string GetFileNameFromKey(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(TileFileFormat, zoom, x, y);
	//    }
	//    public override string GetWebUri(int x, int y, int z, int zoom)
	//    {
	//        return string.Format(WebUriFormatInternal, zoom, x, y);
	//    }
	//    //public override string GetWebUriFormat()
	//    //{
	//    //    return WebUriFormat;
	//    //}
	//    public override int GetMaxZoom()
	//    {
	//        return MaxZoom;
	//    }
	//    public override int GetMinZoom()
	//    {
	//        return MinZoom;
	//    }
	//    public override int GetTilePixelSize()
	//    {
	//        return TilePixelSize;
	//    }
	//}
	//[Table("TileCache")]
	/// <summary>
	/// TileCacheRecord like in the db
	/// </summary>
	public sealed class TileCacheRecord
	{
		//[PrimaryKey]
		public string TileSourceTechName { get { return _tileSourceTechName; } set { _tileSourceTechName = value; } }
		//[PrimaryKey] //, Indexed(Name = "index0", Order = 0, Unique = true)]
		public int X { get { return _x; } set { _x = value; } }
		//[PrimaryKey] //, Indexed(Name = "index0", Order = 0, Unique = true)]
		public int Y { get { return _y; } set { _y = value; } }
		//[PrimaryKey] //, Indexed(Name = "index0", Order = 0, Unique = true)]
		public int Z { get { return _z; } set { _z = value; } }
		//[PrimaryKey] //, Indexed(Name = "index0", Order = 0, Unique = true)]
		public int Zoom { get { return _zoom; } set { _zoom = value; } }
		public string FileName { get { return _fileName; } set { _fileName = value; } }
		//[Ignore]
		public byte[] Img { get { return _img; } set { _img = value; } } // this field has a setter, so SQLite may use it

		private string _tileSourceTechName = string.Empty; // = TileSources.Nokia;
		private int _x = 0;
		private int _y = 0;
		private int _z = 0;
		private int _zoom = 2;
		private string _fileName = "";
		private byte[] _img = null;

		public TileCacheRecord() { } // for the db query
		public TileCacheRecord(string tileSourceTechName, int x, int y, int z, int zoom)
		{
			_tileSourceTechName = tileSourceTechName;
			_x = x;
			_y = y;
			_z = z;
			_zoom = zoom;
		}
		//internal string GetXYZ(int x, int y, int z, int zoom)
		//{
		//    return x + "_" + y + "_" + zoom;
		//}

		internal async static Task<string> GetFilenameFromDbAsync(TileSourceRecord tileSource, int x, int y, int z, int zoom)
		{
			try
			{
				TileCacheRecord record = await DBManager.GetTileRecordAsync(tileSource, x, y, z, zoom).ConfigureAwait(false);
				if (record != null) return record.FileName;
			}
			catch (Exception ex)
			{
				string exception = ex.ToString();
				Debug.WriteLine("ERROR in GetFilenameFromDbAsync(): " + ex.Message + ex.StackTrace);
			}
			return null;
		}
		internal static async Task<RandomAccessStreamReference> GetPixelStreamRefFromImgFolder(StorageFolder imageFolder, string fileName)
		{
			try
			{
				byte[] pixels = null;
				using (var readStream = await imageFolder.OpenStreamForReadAsync(fileName).ConfigureAwait(false))
				{
					pixels = await GetPixelArrayFromByteStream(readStream.AsRandomAccessStream()).ConfigureAwait(false);
				}
				return await GetPixelStreamRefFromPixelArray(pixels).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Logger.Add_TPL(ex.ToString(), Logger.PersistentDataLogFilename);
				return null;
			}
		}
		internal async static Task<RandomAccessStreamReference> GetPixelStreamRefFromByteArray(byte[] imgBytes)
		{
			try
			{
				byte[] pixels = await GetPixelArrayFromByteArray(imgBytes).ConfigureAwait(false);
				return await GetPixelStreamRefFromPixelArray(pixels).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Logger.Add_TPL(ex.ToString(), Logger.PersistentDataLogFilename);
				return null;
			}
		}

		private async static Task<RandomAccessStreamReference> GetPixelStreamRefFromPixelArray(byte[] pixels)
		{
			if (pixels == null || pixels.Length == 0) return null;

			// write pixels into a stream and return a reference to it
			// no Dispose() in the following!
			InMemoryRandomAccessStream inMemoryRandomAccessStream = new InMemoryRandomAccessStream();
			using (IOutputStream outputStream = inMemoryRandomAccessStream.GetOutputStreamAt(0)) // this seems to make it a little more stable
			{
				using (DataWriter writer = new DataWriter(outputStream))
				{
					writer.WriteBytes(pixels);
					await writer.StoreAsync().AsTask().ConfigureAwait(false);
					await writer.FlushAsync().AsTask().ConfigureAwait(false);
					writer.DetachStream(); // otherwise Dispose() will murder the stream
				}
				return RandomAccessStreamReference.CreateFromStream(inMemoryRandomAccessStream);
			}
		}

		private static async Task<byte[]> GetPixelArrayFromByteArray(byte[] source)
		{
			try
			{
				byte[] pixels = null;
				using (InMemoryRandomAccessStream dbStream = new InMemoryRandomAccessStream())
				{
					using (IOutputStream dbOutputStream = dbStream.GetOutputStreamAt(0)) // this seems to make it a little more stable
					{
						using (DataWriter dbStreamWriter = new DataWriter(dbOutputStream))
						{
							dbStreamWriter.WriteBytes(source);
							await dbStreamWriter.StoreAsync().AsTask().ConfigureAwait(false);
							await dbStreamWriter.FlushAsync().AsTask().ConfigureAwait(false);
							dbStreamWriter.DetachStream(); // otherwise Dispose() will murder the stream
						}


						var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(dbStream).AsTask().ConfigureAwait(false);
						//var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(Windows.Graphics.Imaging.BitmapDecoder.PngDecoderId, dbStream).AsTask().ConfigureAwait(false);
						//var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(jpegDecoder.CodecId, dbStream).AsTask().ConfigureAwait(false);
						//LOLLO the image can easily be 250K when the source only takes 10K. We need some compression! I am trying PNG decoder right now.
						// I can also try with the settings below - it actually seems not! I think the freaking output is always 262144 bytes coz it's really all the pixels.

						var pixelProvider = await decoder.GetPixelDataAsync(
							Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8,
							Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
							new Windows.Graphics.Imaging.BitmapTransform(), // { ScaledHeight = 256, ScaledWidth = 256, InterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor }, // { InterpolationMode = ??? }
							Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation,
						//Windows.Graphics.Imaging.ColorManagementMode.ColorManageToSRgb).AsTask().ConfigureAwait(false);
						Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage).AsTask().ConfigureAwait(false);

						pixels = pixelProvider.DetachPixelData();
					}
				}
				return pixels;
			}
			catch (Exception ex)
			{
				Logger.Add_TPL(ex.ToString(), Logger.PersistentDataLogFilename);
				return null;
			}
		}
		private static async Task<byte[]> GetPixelArrayFromByteStream(IRandomAccessStream source)
		{
			try
			{
				byte[] pixels = null;
				var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(source).AsTask().ConfigureAwait(false);

				var pixelProvider = await decoder.GetPixelDataAsync(
					Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8,
					Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
					new Windows.Graphics.Imaging.BitmapTransform(), // { InterpolationMode = ??? }
					Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation,
					//Windows.Graphics.Imaging.ColorManagementMode.ColorManageToSRgb).AsTask().ConfigureAwait(false);
					Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage).AsTask().ConfigureAwait(false);

				pixels = pixelProvider.DetachPixelData();
				return pixels;
			}
			catch (Exception ex)
			{
				Logger.Add_TPL(ex.ToString(), Logger.PersistentDataLogFilename);
				return null;
			}
		}
	}
}
