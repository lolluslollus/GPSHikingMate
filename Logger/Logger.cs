﻿using LolloGPS.Data.Constants;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Utilz
{
    public sealed class Logger
    {
        //logger http://code.msdn.microsoft.com/wpapps/A-logging-solution-for-c407d880

        public enum Severity { Info, Error };

        private static readonly SemaphoreSlimSafeRelease _semaphore = new SemaphoreSlimSafeRelease(1, 1); // , "LOLLOLoggerSemaphore");
        private const long MAX_SIZE_BYTES = 16000;

        static Logger()
        {
            _logsFolder = ApplicationData.Current.LocalFolder.CreateFolderAsync(LogFolderName, CreationCollisionOption.OpenIfExists).AsTask().Result;
        }

        private static StorageFolder _logsFolder = null;
        public const string LogFolderName = "Logs"; // it is placed in the app local folder
        public const string FileErrorLogFilename = "_FileErrorLog.lol";
        public const string ForegroundLogFilename = "_ForegroundLog.lol";
        public const string BackgroundLogFilename = "_BackgroundLog.lol";
        public const string AppExceptionLogFilename = "_AppExceptionLog.lol";
        public const string BackgroundCancelledLogFilename = "_BackgroundCancelledLog.lol";
        public const string PersistentDataLogFilename = "_PersistentData.lol";

        public static async Task<string> ReadAsync(string fileName)
        {
            string result = string.Empty;

            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                result = await Read2Async(fileName).ConfigureAwait(false);
            }
            catch (FileNotFoundException exc0)
            {
                //put up with it: it may be the first time we use this file
                Debug.WriteLine("no worries but: " + exc0.ToString());
            }
            catch (Exception exc)
            {
                if (SemaphoreSlimSafeRelease.IsAlive(_semaphore))
                    Debug.WriteLine("ERROR in Logger.ClearAll(): " + exc.ToString());
            }
            finally
            {
                SemaphoreSlimSafeRelease.TryRelease(_semaphore);
            }
            return result;
        }
        private static async Task<string> Read2Async(string fileName)
        {
            string result = string.Empty;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                StorageFile file = await _logsFolder.GetFileAsync(fileName).AsTask().ConfigureAwait(false);
                if (file != null)
                {
                    using (IInputStream inStream = await file.OpenSequentialReadAsync().AsTask().ConfigureAwait(false))
                    {
                        using (StreamReader streamReader = new StreamReader(inStream.AsStreamForRead()))
                        {
                            result = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            return result;
        }
        public static void Add_TPL(string msg, string fileName,
            Severity severity = Severity.Error,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                string fullMessage = GetFullMsg(severity, memberName, sourceFilePath, sourceLineNumber, msg);
                Debug.WriteLine(fullMessage);
                Task ttt = Task.Run(() => Add2Async(fullMessage, fileName));
            }
            catch (Exception exc)
            {
                Debug.WriteLine("ERROR in Logger.Add_TPL(): " + exc.ToString());
            }
        }

        public static async Task AddAsync(string msg, string fileName,
            Severity severity = Severity.Error,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                string fullMessage = GetFullMsg(severity, memberName, sourceFilePath, sourceLineNumber, msg);
                Debug.WriteLine(fullMessage);
                await Task.Run(() => { return Add2Async(fullMessage, fileName); }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR in Logger.AddAsync(): " + ex.ToString());
            }
        }

        private static string GetFullMsg(Severity severity, string memberName, string sourceFilePath, int sourceLineNumber, string msg)
        {
            if (severity == Severity.Error)
                return string.Format(Environment.NewLine + DateTime.Now + Environment.NewLine
                    + "ERROR in {0}, source {1}, line {2}: {3}", memberName, sourceFilePath, sourceLineNumber, msg);
            else
                return string.Format(Environment.NewLine + DateTime.Now + Environment.NewLine
                    + "INFO from {0}, source {1}, line {2}: {3}", memberName, sourceFilePath, sourceLineNumber, msg);
        }

        private static async Task Add2Async(string msg, string fileName)
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                await UpdateLogAsync(fileName, msg).ConfigureAwait(false);
                //Debug.WriteLine("the thread id is " + Environment.CurrentManagedThreadId + " after the await");
            }
            catch (Exception exc)
            {
                if (SemaphoreSlimSafeRelease.IsAlive(_semaphore))
                    Debug.WriteLine("ERROR in Logger: " + exc.ToString());
            }
            finally
            {
                SemaphoreSlimSafeRelease.TryRelease(_semaphore);
            }
            // await SendEmailWithLogsAsync("lollus@hotmail.co.uk"); // maybe move Logger into the utils and use the right email address and maybe new parameter "sendemailifcrash"
            // On second thought, the email could be annoying and scary. Better leave the option to send it in the "About" panel only.
        }
		private static async Task UpdateLogAsync(string fileName, string msg)
		{
			if (string.IsNullOrEmpty(msg)) return;
			StorageFile file = await _logsFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

			using (var fileStreamForWrite = await file.OpenStreamForWriteAsync().ConfigureAwait(false))
			{
				if (fileStreamForWrite.Length + msg.Length > MAX_SIZE_BYTES)
				{
					fileStreamForWrite.Seek(0, SeekOrigin.Begin);

					using (var reader = new StreamReader(fileStreamForWrite, Encoding.UTF8))
					{
						try
						{
							string current = await reader.ReadToEndAsync();
							string next = current.Substring(Math.Min(msg.Length, current.Length)) + msg; // shift the string to accommodate msg at the end

							fileStreamForWrite.SetLength(next.Length);
							fileStreamForWrite.Seek(0, SeekOrigin.Begin);

							using (var streamWriter = new StreamWriter(fileStreamForWrite, Encoding.UTF8))
							{
								await streamWriter.WriteAsync(next).ConfigureAwait(false);
								await streamWriter.FlushAsync().ConfigureAwait(false);
							}
						}
#pragma warning disable 0168
						catch (Exception ex)
#pragma warning restore 0168
						{

						}
					}
				}
				else
				{
					fileStreamForWrite.Seek(0, SeekOrigin.End);

					using (var streamWriter = new StreamWriter(fileStreamForWrite, Encoding.UTF8))
					{
						await streamWriter.WriteAsync(msg).ConfigureAwait(false);
						await streamWriter.FlushAsync().ConfigureAwait(false);
					}
				}
			}
		}

		public static void ClearAll()
        {
            Task.Run(async delegate
            {
                try
                {
                    await _semaphore.WaitAsync().ConfigureAwait(false);
                    //await _logsFolder.DeleteAsync().AsTask().ConfigureAwait(false);
                    _logsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(LogFolderName, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    if (SemaphoreSlimSafeRelease.IsAlive(_semaphore))
                        Debug.WriteLine("ERROR in Logger.ClearAll(): " + exc.ToString());
                }
                finally
                {
                    SemaphoreSlimSafeRelease.TryRelease(_semaphore);
                }
            });
        }

        //public static async Task SendEmailWithLogsAsync(string recipient)
        //{ // this does not work with WP8.1
        //    EmailRecipient emailRecipient = new EmailRecipient(recipient);

        //    EmailMessage emailMsg = new EmailMessage();
        //    emailMsg.Subject = string.Format("Feedback from {0} with logs", ConstantData.AppName);
        //    emailMsg.To.Add(emailRecipient);
        //    //emailMsg.Body = await ReadAllLogsIntoStringAsync(); // LOLLO this only works with a short body...

        //    string body = await ReadAllLogsIntoStringAsync();

        //    using (var ms = new InMemoryRandomAccessStream())
        //    {
        //        using (var sw = new StreamWriter(ms.AsStreamForWrite(), Encoding.Unicode))
        //        {
        //            await sw.WriteAsync(body);
        //            await sw.FlushAsync();

        //            emailMsg.SetBodyStream(EmailMessageBodyKind.PlainText, RandomAccessStreamReference.CreateFromStream(ms));

        //            await EmailManager.ShowComposeNewEmailAsync(emailMsg).AsTask().ConfigureAwait(false);
        //        }
        //    }
        //}
        //private static async Task<string> ReadAllLogsIntoStringAsync()
        //{
        //    var sb = new StringBuilder();
        //    sb.Append(await ReadOneLogIntoStringAsync(AppExceptionLogFilename).ConfigureAwait(false));
        //    sb.Append(await ReadOneLogIntoStringAsync(BackgroundCancelledLogFilename).ConfigureAwait(false));
        //    sb.Append(await ReadOneLogIntoStringAsync(BackgroundLogFilename).ConfigureAwait(false));
        //    sb.Append(await ReadOneLogIntoStringAsync(FileErrorLogFilename).ConfigureAwait(false));
        //    sb.Append(await ReadOneLogIntoStringAsync(ForegroundLogFilename).ConfigureAwait(false));
        //    sb.Append(await ReadOneLogIntoStringAsync(PersistentDataLogFilename).ConfigureAwait(false));
        //    return sb.ToString();
        //}
        //private static async Task<string> ReadOneLogIntoStringAsync(string filename)
        //{
        //    string output = filename + Environment.NewLine + await ReadAsync(filename).ConfigureAwait(false) + Environment.NewLine;
        //    return output;
        //}
    }
}