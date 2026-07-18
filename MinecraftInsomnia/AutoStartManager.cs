using Microsoft.Win32;
using System;
using System.Reflection;

namespace MinecraftInsomnia
{
    public class AutoStartManager
    {
        private string _keyName;
        private string _executablePath = Assembly.GetEntryAssembly()?.Location ?? string.Empty;

        public AutoStartManager(string keyName)
        {
            _keyName = keyName;
        }

        // Property to check if the program is set to auto-start
        public bool IsAutoStartEnabled
        {
            get
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                var value = key?.GetValue(_keyName);
                return value != null && value.ToString() == _executablePath;
            }

            set
            {
                if (value)
                {
                    AddToStartup();
                }
                else
                {
                    RemoveFromStartup();
                }
            }
        }

        // Method to add the program to startup
        private void AddToStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // Add the program to the startup
                key.SetValue(_keyName, _executablePath);

                key.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding to startup: " + ex.Message);
            }
        }

        // Method to remove the program from startup
        private void RemoveFromStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // Check if the program is set to auto-start
                if (key.GetValue(_keyName) != null)
                {
                    // Remove the program from startup
                    key.DeleteValue(_keyName);
                }

                key.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error removing from startup: " + ex.Message);
            }
        }
    }
}
