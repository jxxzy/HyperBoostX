using System;

namespace HyperBoostX.Services
{
    public sealed class ToastService
    {
        public event EventHandler<string> ToastRequested;
        public void Show(string message) => ToastRequested?.Invoke(this, message);
    }
}
