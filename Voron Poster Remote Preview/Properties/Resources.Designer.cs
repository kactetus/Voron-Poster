﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.34014
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Voron_Poster_Remote_Preview.Properties {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Voron_Poster_Remote_Preview.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Поиск локализованного ресурса типа System.Byte[].
        /// </summary>
        internal static byte[] cryptRDP5 {
            get {
                object obj = ResourceManager.GetObject("cryptRDP5", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на screen mode id:i:1
        ///use multimon:i:0
        ///desktopwidth:i:1024
        ///desktopheight:i:768
        ///session bpp:i:32
        ///winposstr:s:0,0,400,100,1500,1000
        ///compression:i:1
        ///keyboardhook:i:2
        ///audiocapturemode:i:0
        ///videoplaybackmode:i:1
        ///connection type:i:1
        ///networkautodetect:i:0
        ///bandwidthautodetect:i:1
        ///displayconnectionbar:i:1
        ///username:s:PosterTest
        ///enableworkspacereconnect:i:0
        ///disable wallpaper:i:1
        ///allow font smoothing:i:0
        ///allow desktop composition:i:0
        ///disable full window drag:i:1
        ///disable menu anims:i:1
        ///disable themes:i: [остаток строки не уместился]&quot;;.
        /// </summary>
        internal static string RdpFileBegin {
            get {
                return ResourceManager.GetString("RdpFileBegin", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на gatewayhostname:s:
        ///gatewayusagemethod:i:4
        ///gatewaycredentialssource:i:4
        ///gatewayprofileusagemethod:i:0
        ///promptcredentialonce:i:0
        ///gatewaybrokeringtype:i:0
        ///use redirection server name:i:0
        ///rdgiskdcproxy:i:0
        ///kdcproxyname:s:.
        /// </summary>
        internal static string RdpFileEnd {
            get {
                return ResourceManager.GetString("RdpFileEnd", resourceCulture);
            }
        }
    }
}
