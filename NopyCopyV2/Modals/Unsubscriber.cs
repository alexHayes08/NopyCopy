using System;
using System.Collections.Generic;

namespace NopyCopyV2.Modals
{
    /// <summary>
    /// An implemention of the Unsubscriber disposable returned from an
    /// IObserver<T>.Subscribe(...) method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Unsubscriber<T> : IDisposable
    {
        #region Fields

        private bool disposedValue = false;
        private readonly IList<IObserver<T>> observers;
        private readonly IObserver<T> observer;

        #endregion

        #region Constructor

        public Unsubscriber(IList<IObserver<T>> observers, IObserver<T> observer)
        {
            this.observers = observers;
            this.observer = observer;
        }

        #endregion

        #region Methods

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (observers != null && observers.Contains(observer))
                    {
                        observers.Remove(observer);
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion

        #endregion
    }
}
