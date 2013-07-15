using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Microsoft.Win32;

namespace LessXaml.ProjectFile
{
    [Guid("DD51D6A1-1A47-4999-B59B-8ADE73CA9425")]
    [ComVisible(true)]
    public class BuildTool : VsMultipleFileGenerator<string>
    {
        public override IEnumerator<string> GetEnumerator()
        {
            yield return "codebehind";
        }

        protected override string GetFileName(string element, out bool overwrite)
        {
            switch (element)
            {
                case "codebehind":
                    overwrite = false;
                    return Path.ChangeExtension(Path.GetFileName(this.InputFilePath), ".xaml.cs");
                default:
                    throw new Exception();
            }
        }

        public override byte[] GenerateContent(string element)
        {
            try
            {
                switch (element)
                {
                    case "codebehind":
                        {
                            const string Template = @"using System.Windows;
namespace {0}
{{
    public partial class {1}
    {{
        public {1}()
        {{
            InitializeComponent();
        }}
    }}
}}";
                            var root = LessXamlParser.Translate(this.InputFileContents);
                            var classNameAttr = root.Attribute(XName.Get("Class", LessXamlParser.XamlNs));
                            string className = classNameAttr != null ? classNameAttr.Value : null;
                            string nsp = "";
                            if (className != null)
                            {
                                var dotIndex = className.LastIndexOf('.');
                                if (dotIndex != -1)
                                {
                                    nsp = className.Substring(0, dotIndex);
                                    className = className.Substring(dotIndex + 1);
                                }
                            }
                            var result = String.Format(Template, nsp, className);
                            return Encoding.UTF8.GetBytes(result);
                        }

                    default:
                        throw new Exception();
                }
            }
            catch (Exception ex)
            {
                //GeneratorErrorCallback(false, 0, ex.ToString(), 0, 0);
                return Encoding.ASCII.GetBytes("/*\n" + ex.ToString() + "\n*/");
            }
        }

        public override string GetDefaultExtension()
        {
            return ".lx.xaml";
        }

        public override byte[] GenerateSummaryContent()
        {
            var result = LessXamlParser.Translate(this.InputFileContents);
            return Encoding.UTF8.GetBytes(result.ToString());
        }

        #region Registration

        private static Guid CSharpCategory =
            new Guid("{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}");

        private static Guid VBCategory =
            new Guid("{164B10B9-B200-11D0-8C61-00A0C91E29D5}");

        private const string CustomToolName = "LessXaml.ProjectFile";

        private const string CustomToolDescription = "LessXaml file processor";

        private const string KeyFormat
            = @"SOFTWARE\Microsoft\VisualStudio\{0}\Generators\{1}\{2}";

        protected static void Register(Version vsVersion, Guid categoryGuid)
        {
            string subKey = String.Format(KeyFormat,
                vsVersion, categoryGuid.ToString("B"), CustomToolName);

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(subKey))
            {
                if (key == null)
                    return;
                key.SetValue("", CustomToolDescription);
                key.SetValue("CLSID", typeof(BuildTool).GUID.ToString("B"));
                key.SetValue("GeneratesDesignTimeSource", 1);
            }
        }

        protected static void Unregister(Version vsVersion, Guid categoryGuid)
        {
            string subKey = String.Format(KeyFormat,
                vsVersion, categoryGuid.ToString("B"), CustomToolName);

            Registry.LocalMachine.DeleteSubKey(subKey, false);
        }

        public static int[] StudioVersions = { 11 };

        [ComRegisterFunction]
        public static void RegisterClass(Type t)
        {
            foreach (var version in StudioVersions)
            {
                Register(new Version(version, 0), CSharpCategory);
                Register(new Version(version, 0), VBCategory);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterClass(Type t)
        {
            foreach (var version in StudioVersions)
            {
                Unregister(new Version(version, 0), CSharpCategory);
                Unregister(new Version(version, 0), VBCategory);
            }
        }

        #endregion
    }
}
