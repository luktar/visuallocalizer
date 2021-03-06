﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Timers;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Wrapper for SVsStatusbar service. Provides functionality enabling to control VS status bar.
    /// </summary>
    public static class ProgressBarHandler {

        /// <summary>
        /// Instance of the IVsStatusbar service
        /// </summary>
        private static IVsStatusbar statusBar = null;

        /// <summary>
        /// Token used during determinate progress bar to identify this process
        /// </summary>
        private static uint statusBarCookie = 0;

        /// <summary>
        /// Total number of work units that will be performed during the determinate process
        /// </summary>
        private static uint total;                

        /// <summary>
        /// True if the 100% progress bar can be replaced by the "Ready" text
        /// </summary>
        private static bool determinateTimerHit;

        /// <summary>
        /// Obtains SVsStatusbar service instance
        /// </summary>
        private static void CheckInstance() {            
            if (statusBar == null) statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
        }

        /// <summary>
        /// Starts indeterminate animation with given icon
        /// </summary>        
        public static void StartIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants icon) {
            CheckInstance();
            object i = (short)icon;

            try {
                statusBar.Animation(1, ref i);
            } catch { }
        }

        /// <summary>
        /// Stops indeterminate animation with given icon
        /// </summary>
        public static void StopIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants icon) {
            CheckInstance();
            object i = (short)icon;

            try {
                statusBar.Animation(0, ref i);
            } catch { }
        }

        /// <summary>
        /// Starts determinate progress bar animation
        /// </summary>
        /// <param name="totalAmount">Total number of units of work that will be done</param>
        /// <param name="text">Text to display in the status bar</param>
        public static void StartDeterminate(int totalAmount, string text) {
            CheckInstance();

            statusBarCookie = 0;
            total = (uint)totalAmount;

            try {
                statusBar.Progress(ref statusBarCookie, 1, text, 0, total);
            } catch { }
        }

        /// <summary>
        /// Stops determinate progress bar animation
        /// </summary>
        /// <param name="text">Text that will be visible for 500ms</param>
        /// <param name="readyText">Text that will be set to status bar after that</param>
        public static void StopDeterminate(string text, string readyText) {
            CheckInstance();

            int hr = statusBar.Progress(ref statusBarCookie, 1, text, total, total);
            Marshal.ThrowExceptionForHR(hr);

            determinateTimerHit = false;
            Timer t = new Timer();
            t.Enabled = true;
            t.Interval = 500;
            t.Elapsed += new ElapsedEventHandler((o,e) => {
                if (determinateTimerHit) {
                    try {
                        statusBar.Progress(ref statusBarCookie, 0, readyText, total, total);
                    } catch { }

                    t.Stop();
                    t.Dispose();
                }
                determinateTimerHit = true;
            });
            t.Start();            
        }

        /// <summary>
        /// Sets progress of progress bar - this method must be called after StartDeterminate
        /// </summary>
        /// <param name="completed">Number of units of work that has already been completed</param>
        /// <param name="text">Text to be displayed in status bar</param>
        public static void SetDeterminateProgress(int completed, string text) {
            CheckInstance();

            try {
                statusBar.Progress(ref statusBarCookie, 1, text, (uint)completed, total);
            } catch { }
        }
    }
}
