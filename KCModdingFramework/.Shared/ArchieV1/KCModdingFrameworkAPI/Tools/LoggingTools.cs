using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace KaC_Modding_Engine_API.Tools
{
    internal static class LoggingTools
    {

        [STAThread]
        public static MethodBase GetCallingMethod(int frameSkip = 1)
        {
            StackTrace stackTrace = new StackTrace(frameSkip);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            return stackFrames.First().GetMethod();
        }

        /// <summary>
        /// Returns an array of Methods above the currently called method in the method calling stack.
        /// Most recently called will be at in First() position.
        /// WARNING: Runs on main thread.
        /// </summary>
        /// <param name="frameSkip">The number of frames to skip. 1 means it will not include this method.</param>
        /// <returns>The methods that have been called in order.</returns>
        [STAThread]
        public static IEnumerable<MethodBase> GetCallingMethods(int frameSkip = 1)
        {
            // Skips the name of this method
            StackTrace stackTrace = new StackTrace(frameSkip);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            var list = new List<MethodBase>();
            if (stackFrames == null) return list;
            list.AddRange(stackFrames.Select(frame => frame.GetMethod()));

            return list;
        }

        /// <summary>
        /// Gets calling methods as a string joined by delimiter.
        /// Does not include itself or any methods below it.
        /// </summary>
        /// <param name="delimiter">The separator for the method names.</param>
        /// <returns>A string in format: string[delim]string[delim]</returns>
        public static string GetCallingMethodsAsString(string delimiter = ", ")
        {
            // Skips the name of this method
            return string.Join(delimiter, GetCallingMethodsNames(2));
        }

        /// <summary>
        /// Returns an array of Methods above the currently called method in the method calling stack.
        /// Most recently called will be at in First() position.
        /// WARNING: Runs on main thread.
        /// </summary>
        /// <param name="frameSkip">The number of frames to skip. 1 means it will not include this method.</param>
        /// <returns>The methods that have been called in the order: Most recently called first.</returns>
        [STAThread]
        public static IEnumerable<string> GetCallingMethodsNames(int frameSkip = 1)
        {
            return GetCallingMethods(frameSkip).Select(method => method.Name);
        }

        /// <summary>
        /// Gets the namespace of the calling method.
        /// WARNING: Runs on main thread.
        /// </summary>
        /// <param name="frameSkip">The number of frames to skip. 1 means it will not include this method.</param>
        /// <returns>The namespace of the method that called this method.</returns>
        public static string GetCallingNamespace(int frameSkip = 1)
        {
            return GetNamespace(GetCallingMethod(frameSkip));
        }

        public static string GetClassName(MethodBase method)
        {
            return method.DeclaringType.Name;
        }

        public static string GetNamespace(MethodBase method)
        {
            return method.DeclaringType.Namespace;
        }
    }
}