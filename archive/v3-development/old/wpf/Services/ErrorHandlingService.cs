using System;
using System.Diagnostics;

namespace HyperBoostX.Services
{
    public sealed class ErrorHandlingService
    {
        public string ToUserMessage(Exception exception)
        {
            Debug.WriteLine(exception);
            return "HyperBoostX caught an error and kept the UI running.";
        }
    }
}
