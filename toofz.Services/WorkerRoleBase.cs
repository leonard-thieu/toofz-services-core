﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace toofz.Services
{
    public abstract class WorkerRoleBase<TSettings> : ServiceBase, IWorkerRole
        where TSettings : ISettings
    {
        #region Static Members

        static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRoleBase<TSettings>).GetSimpleFullName());

        [ExcludeFromCodeCoverage]
        static void GCCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerRoleBase{TSettings}"/> class.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The specified name is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified name is longer than <see cref="ServiceBase.MaxNameLength"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified name contains forward slash characters.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified name contains backslash characters.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="settings"/> is null.
        /// </exception>
        protected WorkerRoleBase(string serviceName, TSettings settings)
        {
            ServiceName = serviceName;

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            Settings = settings;
        }

        #region Fields

        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        Task run;

        /// <summary>
        /// Gets the settings object.
        /// </summary>
        protected TSettings Settings { get; }

        #endregion

        /// <summary>
        /// Starts the service. This method is intended to be called from console applications.
        /// </summary>
        /// <param name="args">Data passed by the command line.</param>
        public void Start(params string[] args)
        {
            OnStart(args);
        }

        #region OnStart

        /// <summary>
        /// Executes when a Start command is sent to the service by the Service Control Manager (SCM) 
        /// or when the operating system starts (for a service that starts automatically).
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            run = RunAsync(Log, cancellationTokenSource.Token);
            run.ContinueWith(t =>
            {
                Stop();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        #endregion

        #region Run

        internal async Task RunAsync(ILog log, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsyncCore(Idle.StartNew(Settings.UpdateInterval), log, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // Swallow TaskCanceledException. TaskCanceledException signals to exit gracefully.
                }
            }
        }

        internal async Task RunAsyncCore(IIdle idle, ILog log, CancellationToken cancellationToken)
        {
            Settings.Reload();

            try
            {
                await RunAsyncOverride(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
                when (!((ex is TaskCanceledException) ||
                        (ex is TypeInitializationException)))
            {
                log.Error("Failed to complete run due to an error.", ex);
            }

            GCCollect();

            idle.WriteTimeRemaining();

            var remaining = idle.GetTimeRemaining();
            if (remaining > Settings.DelayBeforeGC)
            {
                await Task.Delay(Settings.DelayBeforeGC, cancellationToken).ConfigureAwait(false);
                GCCollect();
            }

            await idle.DelayAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// When overridden in a derived class, provides the main logic to run the update.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>A task that represents the update process.</returns>
        protected abstract Task RunAsyncOverride(CancellationToken cancellationToken);

        #endregion

        #region OnStop

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service Control Manager (SCM).
        /// </summary>
        protected override void OnStop()
        {
            Log.Info("Stopping service...");
            cancellationTokenSource.Cancel();
            run.Wait();
        }

        #endregion
    }
}
