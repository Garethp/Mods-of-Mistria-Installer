﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ModsOfMistriaGUI.App.Lang {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ModsOfMistriaGUI.App.Lang.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Mods Of Mistria Installer.
        /// </summary>
        public static string ApplicationTitle {
            get {
                return ResourceManager.GetString("ApplicationTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not find Fields of Mistria location. Try placing this in the same folder as Fields of Mistria..
        /// </summary>
        public static string CouldNotFindMistria {
            get {
                return ResourceManager.GetString("CouldNotFindMistria", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not find a mods folder. Try creating a folder called &apos;mods&apos; in the Fields of Mistria folder..
        /// </summary>
        public static string CouldNotFindMods {
            get {
                return ResourceManager.GetString("CouldNotFindMods", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fields of Mistria has been detected at: .
        /// </summary>
        public static string FieldsOfMistriaDetectedLocation {
            get {
                return ResourceManager.GetString("FieldsOfMistriaDetectedLocation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Welcome to the Mods of Mistria Installer!.
        /// </summary>
        public static string GreetingText {
            get {
                return ResourceManager.GetString("GreetingText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Install.
        /// </summary>
        public static string InstallButtonText {
            get {
                return ResourceManager.GetString("InstallButtonText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Installing mods....
        /// </summary>
        public static string InstallInProgress {
            get {
                return ResourceManager.GetString("InstallInProgress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} by {1}.
        /// </summary>
        public static string ModByAuthor {
            get {
                return ResourceManager.GetString("ModByAuthor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Some mods require a newer version of the installer. Please update the installer..
        /// </summary>
        public static string ModsRequireNewerVersion {
            get {
                return ResourceManager.GetString("ModsRequireNewerVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Installing will install the following mods:.
        /// </summary>
        public static string ModsWillBeInstalled {
            get {
                return ResourceManager.GetString("ModsWillBeInstalled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No mods found to install.
        /// </summary>
        public static string NoModsToInstall {
            get {
                return ResourceManager.GetString("NoModsToInstall", resourceCulture);
            }
        }
    }
}