//using HarmonyLib;
//using Il2CppInterop.Runtime;
//using Il2CppInterop.Runtime.Injection;
//using Il2CppInterop.Runtime.Runtime;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Text;

//// --- IMPORTANT NOTE IF YOU ARE EDITING THIS FILE ---
//// --- COMMENT OUT THIS ENTIRE FILE AND USE MY CUSTOM IL2CPP BUILD ---
//// --- IF YOU DON'T, THE GAME WILL CRASH WITH UNITY EXPLORER ---

//namespace ZombieTweak2
//{
//    public static class Il2CppInteropVersionHelper
//    {
//        public static string GetIl2CppInteropVersion()
//        {
//            // Find the assembly by name
//            var assembly = AppDomain.CurrentDomain.GetAssemblies()
//                .FirstOrDefault(a => a.GetName().Name == "Il2CppInterop.Common");

//            if (assembly == null)
//                return "Il2CppInterop not loaded";

//            // Get the AssemblyVersion
//            var version = assembly.GetName().Version?.ToString() ?? "Unknown";

//            // Optionally get AssemblyInformationalVersion attribute
//            var infoAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
//            if (infoAttr != null)
//                version = infoAttr.InformationalVersion;

//            // Strip any +suffix (Git hash)
//            if (version.Contains('+'))
//                version = version.Split('+')[0];

//            return version;
//        }
//    }
//    [HarmonyPatch]
//    public static unsafe class il2cppInteropPatches
//    {

//        [HarmonyPatch(typeof(ClassInjector), "GetIl2CppTypeFullName")]
//        [HarmonyPrefix]
//        public static bool GetIl2CppTypeFullNamePatch(Il2CppTypeStruct* typePointer, ref string __result)
//        {
//            var klass = UnityVersionHandler.Wrap((Il2CppClass*)IL2CPP.il2cpp_class_from_type((IntPtr)typePointer));
//            var assembly = UnityVersionHandler.Wrap(UnityVersionHandler.Wrap(klass.Image).Assembly);
//            var fullName = new StringBuilder();
//            var names = new Stack<string>();
//            var declaringType = klass;
//            var outerType = klass;
//            do
//            {
//                names.Push(Marshal.PtrToStringUTF8(declaringType.Name) ?? "");
//                outerType = declaringType;
//            }
//            while ((declaringType = UnityVersionHandler.Wrap(declaringType.DeclaringType)) != default);
//            var namespaceName = outerType.Namespace != IntPtr.Zero ? Marshal.PtrToStringUTF8(outerType.Namespace) ?? "" : "";

//            fullName.Append(namespaceName);
//            if (namespaceName.Length > 0)
//                fullName.Append('.');
//            fullName.Append(string.Join("+", names));

//            var assemblyName = Marshal.PtrToStringUTF8(assembly.Name.Name);
//            if (assemblyName != "mscorlib")
//            {
//                fullName.Append(", ");
//                fullName.Append(assemblyName);
//            }
//            __result = fullName.ToString();
//            return false;
//        }
//    }

//    [HarmonyPatch]
//    public static class AddTypeToLookupPatch
//    {
//        [Flags]
//        public enum Il2CppTypeNameOptions
//        {
//            None = 0,
//            Namespace = 1 << 0,
//            Name = 1 << 1,
//            Assembly = 1 << 2,
//            All = Namespace | Name | Assembly
//        }
//        internal static string GetTypeName(Type type, Il2CppTypeNameOptions options = Il2CppTypeNameOptions.All)
//        {
//            var assembly = type.Assembly;
//            var fullName = new StringBuilder();
//            var names = new Stack<string>();
//            var outerType = type;
//            while (outerType.DeclaringType != null)
//                outerType = outerType.DeclaringType;
//            var namespaceName = outerType.Namespace ?? "";
//            if (options.HasFlag(Il2CppTypeNameOptions.Namespace) && namespaceName.Length > 0)
//            {
//                fullName.Append(namespaceName);
//                fullName.Append('.');
//            }
//            if (options.HasFlag(Il2CppTypeNameOptions.Name) && names.Count > 0)
//                fullName.Append(string.Join("+", names));
//            if (options.HasFlag(Il2CppTypeNameOptions.Assembly))
//            {
//                var assemblyName = assembly.FullName;
//                if (assemblyName != "mscorlib")
//                {
//                    fullName.Append(", ");
//                    fullName.Append(assemblyName);
//                }
//            }
//            return fullName.ToString();
//        } // Could add same options to GetIl2CppTypeFullName too.
//        static MethodBase TargetMethod()
//        {
//            var injectorHelpersType = AccessTools.TypeByName("Il2CppInterop.Runtime.Injection.InjectorHelpers");
//            // Specify the parameters to disambiguate
//            Type[] paramTypes = { typeof(Type), typeof(IntPtr) };
//            return AccessTools.Method(injectorHelpersType, "AddTypeToLookup", paramTypes);
//        }

//        [HarmonyPrefix]
//        public static bool AddTypeToLookup_Prefix(Type type, IntPtr typePointer)
//        {
//            var il2cppType = AccessTools.TypeByName("Il2CppInterop.Runtime.IL2CPP");
//            var getIl2CppImages = AccessTools.Method(il2cppType, "GetIl2CppImages");

//            // Reflect InjectorHelpers.s_ClassNameLookup
//            var injectorHelpers = AccessTools.TypeByName("Il2CppInterop.Runtime.Injection.InjectorHelpers");
//            var s_ClassNameLookup = AccessTools.Field(injectorHelpers, "s_ClassNameLookup");

//            // Get data
//            string klass = GetTypeName(type, Il2CppTypeNameOptions.Name);
//            if (klass.Length == 0) return false;
//            string namespaze = GetTypeName(type, Il2CppTypeNameOptions.Namespace);

//            var attr = Attribute.GetCustomAttribute(
//                type,
//                AccessTools.TypeByName("Il2CppInterop.Runtime.Attributes.ClassInjectionAssemblyTargetAttribute")
//            );

//            IEnumerable<IntPtr> images;
//            if (attr == null)
//                images = (IEnumerable<IntPtr>)getIl2CppImages.Invoke(null, null);
//            else
//            {
//                var getImagePointers = AccessTools.Method(attr.GetType(), "GetImagePointers");
//                images = (IEnumerable<IntPtr>)getImagePointers.Invoke(attr, null);
//            }

//            // Get the dictionary and add entries
//            var dict = (IDictionary<(string, string, IntPtr), IntPtr>)s_ClassNameLookup.GetValue(null);
//            foreach (var image in images)
//                dict.Add((namespaze, klass, image), typePointer);

//            return false; // skip original
//        }
//    }
//}
