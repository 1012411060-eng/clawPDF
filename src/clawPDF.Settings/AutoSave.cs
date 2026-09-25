using System.Text;
using pdfforge.DataStorage;

// Custom Code starts here
// START_CUSTOM_SECTION:INCLUDES
// END_CUSTOM_SECTION:INCLUDES
// Custom Code ends here. Do not edit below

// ! This file is generated automatically.
// ! Do not edit it outside the sections for custom code.
// ! These changes will be deleted during the next generation run

namespace clawSoft.clawPDF.Core.Settings
{
    /// <summary>
    ///     AutoSave allows to create PDF files without user interaction
    /// </summary>
    public class AutoSave
    {
        public AutoSave()
        {
            Init();
        }

        public bool Enabled { get; set; }

        /// <summary>
        ///     Existing files will not be overwritten. Existing filenames automatically get an appendix.
        /// </summary>
        public bool EnsureUniqueFilenames { get; set; }

        public string TargetDirectory { get; set; }

        private void Init()
        {
            Enabled = false;
            EnsureUniqueFilenames = true;
            TargetDirectory = "";
        }

        public void ReadValues(Data data, string path)
        {
            try
            {
                Enabled = bool.Parse(data.GetValue(@"" + path + @"Enabled"));
            }
            catch
            {
                Enabled = false;
            }

            try
            {
                EnsureUniqueFilenames = bool.Parse(data.GetValue(@"" + path + @"EnsureUniqueFilenames"));
            }
            catch
            {
                EnsureUniqueFilenames = true;
            }

            try
            {
                TargetDirectory = ReadTargetDirectory(data.GetValue(@"" + path + @"TargetDirectory"));
            }
            catch
            {
                TargetDirectory = "";
            }
        }

        public void StoreValues(Data data, string path)
        {
            data.SetValue(@"" + path + @"Enabled", Enabled.ToString());
            data.SetValue(@"" + path + @"EnsureUniqueFilenames", EnsureUniqueFilenames.ToString());
            data.SetValue(@"" + path + @"TargetDirectory", Data.EscapeString(TargetDirectory));
        }

        public AutoSave Copy()
        {
            var copy = new AutoSave();

            copy.Enabled = Enabled;
            copy.EnsureUniqueFilenames = EnsureUniqueFilenames;
            copy.TargetDirectory = TargetDirectory;

            return copy;
        }

        public override bool Equals(object o)
        {
            if (!(o is AutoSave)) return false;
            var v = o as AutoSave;

            if (!Enabled.Equals(v.Enabled)) return false;
            if (!EnsureUniqueFilenames.Equals(v.EnsureUniqueFilenames)) return false;
            if (!TargetDirectory.Equals(v.TargetDirectory)) return false;

            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Enabled=" + Enabled);
            sb.AppendLine("EnsureUniqueFilenames=" + EnsureUniqueFilenames);
            sb.AppendLine("TargetDirectory=" + TargetDirectory);

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        // Custom Code starts here
        // START_CUSTOM_SECTION:GENERAL
        private static string ReadTargetDirectory(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // External applications can write native absolute Windows paths. Detect
            // these before Regex.Unescape can turn path segments into e.g. \n or \t.
            var isDriveLetter = (value[0] >= 'A' && value[0] <= 'Z')
                                || (value[0] >= 'a' && value[0] <= 'z');
            if (isDriveLetter && value.Length >= 3 && value[1] == ':' && value[2] == '\\'
                && (value.Length == 3 || value[3] != '\\'))
                return value;

            // A native UNC path starts with two backslashes and has a single
            // server/share separator. Requiring that separator also preserves
            // serialized root-relative paths such as \\folder\\subfolder.
            if (value.Length > 2 && value[0] == '\\' && value[1] == '\\' && value[2] != '\\')
            {
                var separator = value.IndexOf('\\', 2);
                if (separator > 2 && separator + 1 < value.Length && value[separator + 1] != '\\')
                    return value;
            }

            // Keep the existing decoding rules for clawPDF-serialized values.
            return Data.UnescapeString(value);
        }
        // END_CUSTOM_SECTION:GENERAL
        // Custom Code ends here. Do not edit below
    }
}
