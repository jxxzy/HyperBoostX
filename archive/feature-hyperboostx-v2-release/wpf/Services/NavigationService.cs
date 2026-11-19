using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HyperBoostX.Services
{
    public sealed class NavigationService
    {
        private readonly Dictionary<string, Func<UserControl>> _routes = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string key, Func<UserControl> factory) => _routes[key] = factory;

        public UserControl Navigate(string key)
        {
            if (_routes.TryGetValue(key, out var factory))
                return factory();

            if (_routes.TryGetValue("Dashboard", out var dashboard))
                return dashboard();

            throw new InvalidOperationException($"No navigation route registered for {key}.");
        }
    }
}
