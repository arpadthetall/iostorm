/* 
 *	Copyright (C) 2010- ZWaveApi
 *	http://ZWaveApi.net
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation; either version 3, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/lesser.html
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Configuration;
using System.IO;

namespace ZWaveApi.Net
{
    public class ZWaveLog
    {
        static EventLog eventlog;

        private static bool logToEventLog = false;

        private static string LogOutput;
        private static string LogLevel;
        private static string LogFilName;
        private static string LogAppl;
        private static string LogName;

        private static StreamWriter LogFile;

        public delegate void LogHandler(string msg);
        public static LogHandler logHandlerStatic;

        static ZWaveLog()
        {
            /*LogOutput = ConfigurationManager.AppSettings["LogOutput"];
            LogLevel = ConfigurationManager.AppSettings["LogLevel"];
            LogAppl = ConfigurationManager.AppSettings["LogAppl"];
            LogName = ConfigurationManager.AppSettings["LogName"];
            LogFilName = ConfigurationManager.AppSettings["LogPath"] + "\\" + LogName;*/

            if (LogOutput.ToLower().Contains("fil"))
            {
                StreamWriter LogFile = new System.IO.StreamWriter(LogFilName + ".log", true);
            }

            if (LogOutput.ToLower().Contains("eventlog"))
            {
                eventlog = new EventLog();

                if (!EventLog.SourceExists(LogAppl))
                {
                    //Creating new Log, (it will appear as a tree node in windows event log)            
                    EventLog.CreateEventSource(LogAppl, LogName);
                }
                eventlog.Source = LogAppl;
                eventlog.EnableRaisingEvents = true;
            }
        }

        ~ZWaveLog()
        {
            if (LogOutput.ToLower().Contains("fil"))
            {
                LogFile.Close();
            }
        }

        public static bool toWcf()
        {
            if (LogOutput.ToLower().Contains("wcf"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Add A message handler to this class message handler.
        /// </summary>
        /// <param name="messageHandler">The message handler that is to be add</param>
        public static void SubscribeToMessages(LogHandler logHandler)
        {
            logHandlerStatic += logHandler;
        }

        /// <summary>
        /// Add a message with a byte[] to the eventlog, 
        /// and to console if en debug mode
        /// </summary>
        /// <param name="Message">The message to write</param>
        /// <param name="buffer">The buffer to write.</param>
        public static void addMessageBuffer(string Message, byte[] buffer)
        {
            string message = Message;

            foreach (byte b in buffer)
            {
                message += b.ToString("X2") + " ";
            }

            AddEvent(message);
        }

        private static void AddEvent(string message, EventLogEntryType eventLogEntryType)
        {
            if (LogOutput.ToLower().Contains("eventlog"))
                eventlog.WriteEntry(message: message, type: eventLogEntryType);

            if (LogOutput.ToLower().Contains("screen"))
                Console.WriteLine(DateTime.Now.ToString() + ":" + eventLogEntryType.ToString() + ":" + message);

            if (LogOutput.ToLower().Contains("wcf"))
                logHandlerStatic(eventLogEntryType.ToString() + ":" + message);

            if (LogOutput.ToLower().Contains("fil"))
            {
                StreamWriter LogFile = new System.IO.StreamWriter(LogFilName + "." + eventLogEntryType.ToString(), true);
                    
                try
                {
                    LogFile.WriteLine(DateTime.Now.ToString() + ":" + eventLogEntryType.ToString() + ":" + message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    LogFile.Flush();
                    LogFile.Close();
                }
            }
        }

        /// <summary>
        /// Log a event to the eventLog
        /// </summary>
        /// <param name="Message">The error that has to be log.</param>
        public static void AddEvent(string message)
        {
            AddEvent(message: message, eventLogEntryType: EventLogEntryType.Information);
        }

        /// <summary>
        /// Log a exception to the eventLog
        /// </summary>
        /// <param name="Message">The exception that has to be log.</param>
        public static void AddException(string message)
        {
            AddEvent(message: message, eventLogEntryType: EventLogEntryType.Error);
        }

        /// <summary>
        /// Log a exception to the eventLog
        /// </summary>
        /// <param name="Message">The exception that has to be log.</param>
        public static void AddException(Exception e)
        {
            AddEvent(message: e.Message, eventLogEntryType: EventLogEntryType.Error);
        }

        /// <summary>
        /// Log a warning to the eventLog
        /// </summary>
        /// <param name="message">The warining that has to be log.</param>
        public static void AddWarning(string message)
        {
            AddEvent(message: message, eventLogEntryType: EventLogEntryType.Warning);
        }
    }
}
