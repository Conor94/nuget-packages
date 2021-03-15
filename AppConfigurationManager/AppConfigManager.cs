using System;
using System.Configuration;
using System.IO;

namespace AppConfigurationManager
{
    /// <summary>
    /// Manages a custom <see cref="ConfigurationSection"/>. Inherit from <see cref="ConfigurationSection"/>
    /// to create a custom configuration section.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Init(Environment.SpecialFolder)"/> to initialize this class before calling any other methods.
    /// </remarks>
    public static class AppConfigManager<TSection> where TSection : ConfigurationSection, new()
    {
        #region Fields
        private static readonly string mSectionName;
        private static string mConfigPath;
        private static Configuration mConfig;
        #endregion

        #region Constructor
        /// <summary>
        /// Static constructor for the <see cref="AppConfigManager{TSection}"/> class.
        /// </summary>
        static AppConfigManager()
        {
            mSectionName = typeof(TSection).Name;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the configuration file if it is not already created.
        /// <para/>
        /// The file will be created as follows: "{SpecialFolder}\{AssemblyName}\{AssemblyName}.exe.config", 
        /// where {SpecialFolder} is the path to the special system folder and {AssemblyName} is the name
        /// of the assembly that called this method.
        /// </summary>
        /// <remarks>
        /// This method must be called before calling <see cref="GetSection"/> or <see cref="Save"/>.
        /// </remarks>
        /// <param name="specialFolder">Path that the configuration file will be saved to.</param>
        /// <exception cref="ConfigurationErrorsException"/>
        public static void Init(Environment.SpecialFolder specialFolder, string assemblyName)
        {
            // Create the path to the config file
            mConfigPath = Path.Combine(new string[]
            {
                Environment.GetFolderPath(specialFolder),
                assemblyName,
                $"{assemblyName}.exe.config"
            });
            // Change the apps configuration file path
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", mConfigPath);

            // Create the configuration file if it hasn't already been created
            mConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None); // Get the configuration file
            if (mConfig.Sections[mSectionName] == null)
            {
                TSection section = new TSection();
                mConfig.Sections.Add(mSectionName, section);
                mConfig.Save(ConfigurationSaveMode.Full, true);
            }
        }

        /// <summary>
        /// Gets the configuration section of type <typeparamref name="TSection"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Init(Environment.SpecialFolder)"/> must be called before calling this method.
        /// </remarks>
        /// <returns><typeparamref name="TSection"/> if the section is found and <see langword="null"/> if the
        /// section could not be found.</returns>
        public static TSection GetSection()
        {
            // Get the configuration file and return it
            mConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            return mConfig.GetSection(mSectionName) as TSection;
        }

        /// <summary>
        /// Saves the configuration file. A <see cref="ConfigurationErrorsException"/> is thrown
        /// if the file cannot be saved.
        /// </summary>
        /// <remarks>
        /// <see cref="Init(Environment.SpecialFolder)"/> must be called before calling this method.
        /// </remarks>
        /// <exception cref="ConfigurationErrorsException"/>
        public static void Save()
        {
            mConfig.Save(ConfigurationSaveMode.Modified);
        }
        #endregion
    }
}
