using System;
using System.Diagnostics;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;

namespace ZombieTweak2
{
    [Obsolete]
    internal static class zLogger
    { 
        //This is no longer used.  I think I can just delete it?  but keeping it here for a bit just in case.
        
        private static readonly ManualLogSource _Logger;

        static zLogger()
        {
            _Logger = new ManualLogSource(VersionInfo.RootNamespace);
            BepInEx.Logging.Logger.Sources.Add(_Logger);
        }

        private static string Format(object msg) => msg.ToString();
        public static void Info(BepInExInfoLogInterpolatedStringHandler handler) => _Logger.LogInfo(handler);
        public static void Info(string str) => _Logger.LogMessage(str);
        public static void Info(object data) => _Logger.LogMessage(Format(data));
        public static void Debug(BepInExDebugLogInterpolatedStringHandler handler) => _Logger.LogDebug(handler);
        public static void Debug(string str) => _Logger.LogDebug(str);
        public static void Debug(object data) => _Logger.LogDebug(Format(data));
        public static void Error(BepInExErrorLogInterpolatedStringHandler handler) => _Logger.LogError(handler);
        public static void Error(string str) => _Logger.LogError(str);
        public static void Error(object data) => _Logger.LogError(Format(data));
        public static void Fatal(BepInExFatalLogInterpolatedStringHandler handler) => _Logger.LogFatal(handler);
        public static void Fatal(string str) => _Logger.LogFatal(str);
        public static void Fatal(object data) => _Logger.LogFatal(Format(data));
        public static void Warn(BepInExWarningLogInterpolatedStringHandler handler) => _Logger.LogWarning(handler);
        public static void Warn(string str) => _Logger.LogWarning(str);
        public static void Warn(object data) => _Logger.LogWarning(Format(data));

        [Conditional("DEBUG")]
        public static void DebugOnly(object data)
        {
#if DEBUG
            _Logger.LogDebug(Format(data));
#endif
        }
    }
}