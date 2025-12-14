using System.Text;
using clawSoft.clawPDF.Core.Settings.Enums;
using pdfforge.DataStorage;

namespace clawSoft.clawPDF.Core.Settings
{
    public class UiEmailButtonSettings
    {
        public UiEmailButtonSettings()
        {
            Init();
        }

        public UiEmailButtonMode Mode { get; set; }

        public string CommandPath { get; set; }

        public string CommandArguments { get; set; }

        public bool WaitForExit { get; set; }

        public bool HideWindow { get; set; }

        private void Init()
        {
            Mode = UiEmailButtonMode.Default;
            CommandPath = "";
            CommandArguments = "";
            WaitForExit = true;
            HideWindow = true;
        }

        public void ReadValues(Data data, string path)
        {
            try
            {
                Mode = (UiEmailButtonMode)int.Parse(data.GetValue(@"" + path + @"Mode"));
            }
            catch
            {
                Mode = UiEmailButtonMode.Default;
            }

            try
            {
                CommandPath = Data.UnescapeString(data.GetValue(@"" + path + @"CommandPath"));
            }
            catch
            {
                CommandPath = "";
            }

            try
            {
                CommandArguments = Data.UnescapeString(data.GetValue(@"" + path + @"CommandArguments"));
            }
            catch
            {
                CommandArguments = "";
            }

            try
            {
                WaitForExit = bool.Parse(data.GetValue(@"" + path + @"WaitForExit"));
            }
            catch
            {
                WaitForExit = true;
            }

            try
            {
                HideWindow = bool.Parse(data.GetValue(@"" + path + @"HideWindow"));
            }
            catch
            {
                HideWindow = true;
            }
        }

        public void StoreValues(Data data, string path)
        {
            data.SetValue(@"" + path + @"Mode", ((int)Mode).ToString());
            data.SetValue(@"" + path + @"CommandPath", Data.EscapeString(CommandPath));
            data.SetValue(@"" + path + @"CommandArguments", Data.EscapeString(CommandArguments));
            data.SetValue(@"" + path + @"WaitForExit", WaitForExit.ToString());
            data.SetValue(@"" + path + @"HideWindow", HideWindow.ToString());
        }

        public UiEmailButtonSettings Copy()
        {
            var copy = new UiEmailButtonSettings
            {
                Mode = Mode,
                CommandPath = CommandPath,
                CommandArguments = CommandArguments,
                WaitForExit = WaitForExit,
                HideWindow = HideWindow
            };

            return copy;
        }

        public override bool Equals(object o)
        {
            if (!(o is UiEmailButtonSettings)) return false;
            var v = o as UiEmailButtonSettings;

            if (!Mode.Equals(v.Mode)) return false;
            if (!string.Equals(CommandPath, v.CommandPath)) return false;
            if (!string.Equals(CommandArguments, v.CommandArguments)) return false;
            if (!WaitForExit.Equals(v.WaitForExit)) return false;
            if (!HideWindow.Equals(v.HideWindow)) return false;

            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Mode=" + Mode);
            sb.AppendLine("CommandPath=" + CommandPath);
            sb.AppendLine("CommandArguments=" + CommandArguments);
            sb.AppendLine("WaitForExit=" + WaitForExit);
            sb.AppendLine("HideWindow=" + HideWindow);

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
