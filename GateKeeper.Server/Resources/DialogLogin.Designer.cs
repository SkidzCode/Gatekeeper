﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GateKeeper.Server.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class DialogLogin {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal DialogLogin() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GateKeeper.Server.Resources.DialogLogin", typeof(DialogLogin).Assembly);
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
        ///   Looks up a localized string similar to There was an error during the login process.
        /// </summary>
        public static string LoginError {
            get {
                return ResourceManager.GetString("LoginError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid credentials.
        /// </summary>
        public static string LoginInvalid {
            get {
                return ResourceManager.GetString("LoginInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Refresh Token.
        /// </summary>
        public static string LoginInvalidRefreshToken {
            get {
                return ResourceManager.GetString("LoginInvalidRefreshToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have exceeded the maximum number of login attempts. Please try again after 30 minutes..
        /// </summary>
        public static string LoginMaxAttempts {
            get {
                return ResourceManager.GetString("LoginMaxAttempts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while logging out from device.
        /// </summary>
        public static string LogoutDeviceError {
            get {
                return ResourceManager.GetString("LogoutDeviceError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Logged out successfully.
        /// </summary>
        public static string LogoutDeviceSucess {
            get {
                return ResourceManager.GetString("LogoutDeviceSucess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred during logout.
        /// </summary>
        public static string LogoutError {
            get {
                return ResourceManager.GetString("LogoutError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to logout from the specified device.
        /// </summary>
        public static string LogoutFailure {
            get {
                return ResourceManager.GetString("LogoutFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} token(s) revoked successfully.
        /// </summary>
        public static string LogoutRevokeToken {
            get {
                return ResourceManager.GetString("LogoutRevokeToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There was an error loading your profile.
        /// </summary>
        public static string ProfileLoadError {
            get {
                return ResourceManager.GetString("ProfileLoadError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while retrieving active sessions.
        /// </summary>
        public static string SessionGetError {
            get {
                return ResourceManager.GetString("SessionGetError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to test.
        /// </summary>
        public static string test {
            get {
                return ResourceManager.GetString("test", resourceCulture);
            }
        }
    }
}
