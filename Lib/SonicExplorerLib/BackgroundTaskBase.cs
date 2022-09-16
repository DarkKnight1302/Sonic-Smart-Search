namespace SonicExplorerLib
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.Background;

    /// <summary>
    /// Base class for all background tasks.
    /// </summary>
    /// <remarks>
    /// Note: Register, Unregister, and Reregister are all done in a cross process atomic way. Therefore, if a
    /// background task is doing any of these oporations at the same time as another backgrount task or foreground app,
    /// only one of them will continue while the others are blocked awaiting the release by the other processes.
    /// </remarks>
    public abstract class BackgroundTaskBase
    {
        private static readonly HashSet<BackgroundTaskCancellationReason> NormalCancellationReasons = new HashSet<BackgroundTaskCancellationReason>()
        {
            BackgroundTaskCancellationReason.ConditionLoss,
            BackgroundTaskCancellationReason.EnergySaver,
            BackgroundTaskCancellationReason.LoggingOff,
            BackgroundTaskCancellationReason.ResourceRevocation,
            BackgroundTaskCancellationReason.ServicingUpdate,
            BackgroundTaskCancellationReason.SystemPolicy,
            BackgroundTaskCancellationReason.Terminating,
            BackgroundTaskCancellationReason.Uninstall,
        };

        private static BackgroundTaskCancellationReason? stashedReasonForWatsonDiagnostics = default;

        /// <summary>
        /// Gets a value indicating whether this background task is implements <see cref="RunAsync"/> (true) or <see cref="RunSync"/> (false).
        /// </summary>
        protected abstract bool IsAsync { get; }

        /// <summary>
        /// Gets the task's name.
        /// </summary>
        protected abstract string TaskName { get; }

        /// <summary>
        /// Gets the task's entry point. If null, background task will be run in the foreground app's process.
        /// </summary>
        protected abstract string EntryPoint { get; }

        /// <summary>
        /// Gets a list of TriggerConditions for the background task.
        /// </summary>
        protected virtual IEnumerable<IBackgroundCondition> TriggerConditions => Enumerable.Empty<IBackgroundCondition>();

        /// <summary>
        /// Gets the action to perform after successful registering of the task.
        /// </summary>
        protected virtual Action PostRegisterAction => null;

        /// <summary>
        /// Performs the work of a background task. The system calls this method when the
        /// associated background task has been triggered.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task. The system creates this instance
        /// when the task has been triggered to run.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task RunImplAsync(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskCancellationReason? cancellationReason = default;
            try
            {
                Debug.WriteLine($"BackgroundTask '{this.TaskName}' started.");

                _ = taskInstance ?? throw new ArgumentNullException(nameof(taskInstance));

                using CancellationTokenSource cancellationSource = new CancellationTokenSource();
                taskInstance.Canceled += (s, r) =>
                {
                    cancellationReason = r;
                    cancellationSource.Cancel(true);
                };

                if (this.IsAsync)
                {
                    using DisposableBackgroundTaskDeferral disposableDeferral = new DisposableBackgroundTaskDeferral(taskInstance.GetDeferral());
                    await this.RunAsync(taskInstance, cancellationSource.Token).ConfigureAwait(false);
                }
                else
                {
                    this.RunSync(taskInstance, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                if (cancellationReason.HasValue && NormalCancellationReasons.Contains(cancellationReason.Value))
                {
                    // In these situations do not crash process; do not Watson.
                    return;
                }

                stashedReasonForWatsonDiagnostics = cancellationReason;
                throw;
            }
            finally
            {
                Debug.WriteLine($"BackgroundTask '{this.TaskName}' ended.");
            }
        }

        /// <summary>
        /// Unregister and reregister the background task.
        /// </summary>
        public void Reregister()
        {
            using Semaphore sem = this.AcquireSystemSem();
            try
            {
                this.Unregister(true);
                this.Register(true);
            }
            finally
            {
                sem?.Release();
            }
        }

        /// <summary>
        /// The register the background task.
        /// </summary>
        public void Register() => this.Register(false);

        /// <summary>
        /// The unregister the background task.
        /// </summary>
        public void Unregister() => this.Unregister(false);

        /// <summary>
        /// Performs the background task operation asynchronously.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task. The system creates this
        /// instance when the task has been triggered to run.</param>
        /// <param name="cancelToken">For handling task cancellation.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected virtual Task RunAsync(IBackgroundTaskInstance taskInstance, CancellationToken cancelToken)
        {
            if (this.IsAsync)
            {
                // The derived class did not override this method and should have.
                throw new InvalidOperationException(
                    $"{this.GetType().Name} must override {nameof(this.RunAsync)} or set {nameof(this.IsAsync)} to false.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs the background task operation synchronously.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task. The system creates this
        /// instance when the task has been triggered to run.</param>
        /// <param name="cancelToken">For handling task cancellation.</param>
        protected virtual void RunSync(IBackgroundTaskInstance taskInstance, CancellationToken cancelToken)
        {
            if (!this.IsAsync)
            {
                // The derived class did not override this method and should have.
                throw new InvalidOperationException(
                    $"{this.GetType().Name} must override {nameof(this.RunSync)} or set {nameof(this.IsAsync)} to true.");
            }
        }

        /// <summary>
        /// Makes the trigger for this task.
        /// </summary>
        /// <returns>The new trigger.</returns>
        protected abstract IBackgroundTrigger MakeTrigger();

        /// <summary>
        /// Gets a value indicating whether the task should be registered.
        /// </summary>
        /// <returns>The new trigger.</returns>
        protected abstract bool IsRegisterAllowed();

        private BackgroundTaskBuilder CreateBuilder()
        {
            IBackgroundTrigger trigger = this.MakeTrigger();
            if (trigger is null)
            {
                return null;
            }

            BackgroundTaskBuilder result = new BackgroundTaskBuilder()
            {
                Name = this.TaskName,
                TaskEntryPoint = this.EntryPoint,
            };

            result.SetTrigger(trigger);
            return result;
        }

        private void Unregister(bool isSemHeld)
        {
            using Semaphore sem = isSemHeld ? null : this.AcquireSystemSem();
            try
            {
                IEnumerable<IBackgroundTaskRegistration> regs = BackgroundTaskRegistration.AllTasks.Values.Where(
        t => string.Equals(t.Name, this.TaskName, StringComparison.Ordinal));
                foreach (IBackgroundTaskRegistration reg in regs)
                {
                    reg.Unregister(true);
                }
            }
            finally
            {
                sem?.Release();
            }
        }

        private void Register(bool isSemHeld)
        {
            using Semaphore sem = isSemHeld ? null : this.AcquireSystemSem();

            try
            {
                if (!this.IsRegisterAllowed())
                {
                    this.Unregister(true);
                    return;
                }

                bool isAlreadyRegistered = BackgroundTaskRegistration.AllTasks.Values.Any(
                    t => string.Equals(t.Name, this.TaskName, StringComparison.Ordinal));
                if (isAlreadyRegistered)
                {
                    Debug.WriteLine($"Already Registered: {this.TaskName}");
                    return;
                }

                BackgroundTaskBuilder builder = this.CreateBuilder();
                if (builder is null)
                {
                    Debug.WriteLine($"Unable to build background task: {this.TaskName}");
                    return;
                }

                foreach (IBackgroundCondition cond in this.TriggerConditions)
                {
                    builder.AddCondition(cond);
                }

                try
                {
                    builder.Register();
                    Debug.WriteLine($"Registered: {this.TaskName}");
                    this.PostRegisterAction?.Invoke();
                }
                catch (ArgumentException argEx)
                {
                    // ArgumentException happens some times.  Not sure why.  We'll retry next time.
                    // This is HResult 0x80070057
                    Debug.Fail($"Task '{builder.Name}' NOT Registered {argEx}.  We'll try on next app launch.");
                }
                catch (Exception e)
                {
                    // EptSNotRegistered happns when we run out of end points.
                    // ErrorBadPathname happens for some unknown reason.
                    // RegDbEClassNotReg happens infrequently. We are not sure why.
                    // RegDbEInvalidValue happens infrequently. We are not sure why.
                    // ErrorElementNotFound happens infrequently. We are not sure why.
                    Debug.Fail($"Task '{builder.Name}' NOT Registered {e}.  We'll try on next app launch.");
                }
            }
            finally
            {
                sem?.Release();
            }
        }

        private Semaphore AcquireSystemSem()
        {
            Semaphore sem = new Semaphore(0, 1, $"SurfaceAppBgTaskReg_{this.TaskName}", out bool isAllowedToContinue);

            if (!isAllowedToContinue)
            {
                sem.WaitOne();
            }

            return sem;
        }
    }
}
