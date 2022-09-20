using Prism.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SonicExplorerLib
{
    public class SettingsContainer
    {
        private ApplicationDataContainer container;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsContainer"/> class.
        /// </summary>
        /// <param name="container">The underlying settings container.</param>
        private SettingsContainer()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            this.container = localSettings.CreateContainer("localContainer", ApplicationDataCreateDisposition.Always);
        }

        public static Lazy<SettingsContainer> instance = new Lazy<SettingsContainer>(() => new SettingsContainer());

        public void SetValue<T>(string key, T value)
        {
            this.container.Values[key] = value;
        }

        public T GetValue<T>(string key)
        {
            if (this.container.Values.TryGetValue(key, out T value))
            {
                return value;
            }
            return default(T);
        }
    }
}
