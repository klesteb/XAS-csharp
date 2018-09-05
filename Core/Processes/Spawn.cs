﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using XAS.Core.Logging;
using XAS.Core.Security;
using XAS.Core.Exceptions;
using XAS.Core.Configuration;

namespace XAS.Core.Processes {

    /// <summary>
    /// Spawn a process and keep it running.
    /// </summary>
    /// 
    public class Spawn {

        private Int32 retries = 0;
        private Int32 exitCode = 0;

        private readonly ILogger log = null;
        private readonly Process process = null;
        private readonly ISecurity secure = null;
        private readonly SpawnInfo spawnInfo = null;
        private readonly IConfiguration config = null;
        private readonly IErrorHandler handler = null;
        private readonly ProcessStartInfo startInfo = null;

        /// <summary>
        /// Contructor.
        /// </summary>
        /// <param name="config">An IConfiguration object.</param>
        /// <param name="handler">An IErrorHandler object.</param>
        /// <param name="logFactory">An ILoggerFactory object.</param>
        /// <param name="spawnInfo">An SpawnInfo object.</param>
        /// 
        public Spawn(IConfiguration config, IErrorHandler handler, ILoggerFactory logFactory, SpawnInfo spawnInfo) {
 
            this.config = config;
            this.handler = handler;
            this.spawnInfo = spawnInfo;
            this.secure = new Secure();
            this.log = logFactory.Create(typeof(Spawn));

            // parse the command line

            string[] args = Utils.ParseCommandLine(spawnInfo.Command);
            int length = args.Length - 1;

            // set up the process

            this.process = new Process();
            this.startInfo = new ProcessStartInfo() {
                FileName = args[0],
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = spawnInfo.WorkingDirectory,
                Arguments = String.Join(" ", args.Skip(1).Take(length).ToArray()),
            };

            // set user context, if any

            if ((! String.IsNullOrEmpty(spawnInfo.Username) && (! String.IsNullOrEmpty(spawnInfo.Password)))) {

                startInfo.LoadUserProfile = true;
                startInfo.Domain = spawnInfo.Domain;
                startInfo.UserName = spawnInfo.Username;
                startInfo.Password = secure.MakeSecureString(spawnInfo.Password);

            }

            // add environment variables, if any

            foreach (KeyValuePair<String, String> env in spawnInfo.Environment) {

                // replace existing environment variables

                if (startInfo.Environment.ContainsKey(env.Key)) {

                    startInfo.Environment.Remove(env.Key);

                }

                startInfo.Environment.Add(env.Key, env.Value);

            }

            process.EnableRaisingEvents = true;
            process.StartInfo = startInfo;
            process.Exited += ExitHandler;

            if (spawnInfo.StderrHandler != null) {

                process.ErrorDataReceived += spawnInfo.StderrHandler;

            } else {

                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {

                    if (! String.IsNullOrEmpty(e.Data)) {

                        log.Error(e.Data.Trim());

                    }

                };

            }

            if (spawnInfo.StdoutHandler != null) {

                process.OutputDataReceived += spawnInfo.StdoutHandler;

            } else {

                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {

                    if (! String.IsNullOrEmpty(e.Data)) {

                        log.Info(e.Data.Trim());

                    }

                };

            }

            if (spawnInfo.ExitHandler != null) {

                process.Exited += spawnInfo.ExitHandler;

            }

            if (spawnInfo.AutoStart) {

                Start();

            }

        }

        /// <summary>
        /// Start a process.
        /// </summary>
        /// 
        public void Start() {

            log.Trace("Entering Start()");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            log.Trace("Leaving Start()");

        }

        /// <summary>
        /// Stop a process.
        /// </summary>
        /// 
        public void Stop() {

            log.Trace("Entering Stop()");

            process.CloseMainWindow();

            log.Trace("Leaving Stop()");

        }

        /// <summary>
        /// Stat a process.
        /// </summary>
        /// <returns>true if the process still exists.</returns>
        /// 
        public Boolean Stat() {

            log.Trace("Entering Stat()");

            bool stat = false;

            try {
            
                var junk = System.Diagnostics.Process.GetProcessById(process.Id);
                stat = true;

            } catch { }

            log.Trace("Leaving Stat()");

            return stat;

        }

        /// <summary>
        /// Pause a process.
        /// </summary>
        /// 
        public void Pause() {

            log.Trace("Entering Pause()");
            log.Trace("Leaving Pause()");
        }

        /// <summary>
        /// Resume a process.
        /// </summary>
        /// 
        public void Resume() {

            log.Trace("Entering Resume()");
            log.Trace("Leaving Resume()");

        }

        /// <summary>
        /// Kill a process.
        /// </summary>
        /// 
        public void Kill() {

            log.Trace("Entering Kill()");

            process.Kill();

            log.Trace("Leaving Kill()");

        }

        /// <summary>
        /// Return the processes exit code.
        /// </summary>
        /// <returns>A integer value.</returns>
        /// 
        public Int32 ExitCode() {

            return exitCode;

        }

        #region Private Methods

        private void ExitHandler(object sender, EventArgs e) {

            log.Trace("Entering ExitHandler()");

            exitCode = process.ExitCode;

            // do some cleanup

            process.CancelOutputRead();
            process.CancelErrorRead();
            process.Close();

            // restart logic

            retries++;

            if (! spawnInfo.ExitCodes.Contains(exitCode)) {

                if ((spawnInfo.AutoRestart) && (retries <= spawnInfo.ExitRetries)) {

                    Start();

                }

            }

            log.Trace("Leaving ExitHandler()");

        }

        #endregion

    }

}
