using System;

namespace PrismMvvmBase.Bindable
{
    public abstract class ModelBase : DataErrorBindableBase, IDisposable
    {
        protected bool mIsDisposed;

        private bool mIsSelected;

        public bool IsSelected
        {
            get => mIsSelected;
            set => SetProperty(ref mIsSelected, value);
        }

        public ModelBase() : base()
        {
        }

        public void Dispose() => Dispose(true);

        protected abstract void Dispose(bool disposing);
    }
}