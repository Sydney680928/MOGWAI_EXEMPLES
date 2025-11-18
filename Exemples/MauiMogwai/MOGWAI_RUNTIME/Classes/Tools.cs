using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MOGWAI_RUNTIME.Classes
{
    internal class Tools
    {
        public static void SuspendAutoPowerOff()
        {
#if ANDROID

            if (Platform.CurrentActivity is MainActivity activity)
            {
                activity.SetScreenOn();
            }

#elif IOS

            Platforms.iOS.IosTools.SuspendAutoPowerOff();

#endif
        }

        public static void ResumeAutoPowerOff()
        {
#if ANDROID

            if (Platform.CurrentActivity is MainActivity activity)
            {
                activity.LeaveScreenOff();
            }

#elif IOS

            Platforms.iOS.IosTools.ResumeAutoPowerOff();

#endif
        }

        public static string? GetStringFromResource(string resource)
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"MOGWAI_RUNTIME.Resources.Raw.{resource}");

                if (stream != null)
                {
                    var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
