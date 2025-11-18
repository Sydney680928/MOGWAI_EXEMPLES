using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace MOGWAI_RUNTIME.Platforms.iOS
{
    internal class IosTools
    {
        internal static void SuspendAutoPowerOff()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UIApplication.SharedApplication.IdleTimerDisabled = true;
            });
        }

        internal static void ResumeAutoPowerOff()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UIApplication.SharedApplication.IdleTimerDisabled = false;
            });
        }
    }
}
