namespace SonicExplorerLib
{
    using System;

    using Windows.ApplicationModel.Background;

    /// <summary>
    /// A disposable deferral that automatically completest itself when disposed.
    /// </summary>
    public class DisposableBackgroundTaskDeferral : IDisposable
    {
        private BackgroundTaskDeferral deferral;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableBackgroundTaskDeferral"/> class.
        /// </summary>
        /// <param name="deferral">The <see cref="BackgroundTaskDeferral"/> to manage.</param>
        public DisposableBackgroundTaskDeferral(BackgroundTaskDeferral deferral) => this.deferral = deferral;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of this object.
        /// </summary>
        /// <param name="isCalledFromDispose">True when called from <see cref="Dispose()"/> method.</param>
        protected virtual void Dispose(bool isCalledFromDispose)
        {
            if (isCalledFromDispose)
            {
                this.deferral?.Complete();
            }
        }
    }
}
