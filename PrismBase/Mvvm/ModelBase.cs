﻿using System;

namespace PrismBase.Mvvm
{
    public abstract class ModelBase : DataErrorBindableBase, IDisposable
    {
        protected bool mIsDisposed;

        public ModelBase() : base()
        {
        }

        public void Dispose() => Dispose(true);

        protected abstract void Dispose(bool disposing);
    }
}