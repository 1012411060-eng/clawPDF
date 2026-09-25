using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using clawSoft.clawPDF.Core.Settings;
using NUnit.Framework;
using pdfforge.DataStorage;
using pdfforge.DataStorage.Storage;
#if NETFRAMEWORK
using Microsoft.Win32;
#endif

namespace clawPDF.Settings.Tests
{
    [TestFixture]
    public class AutoSaveTests
    {
        private const string ProfilePath = @"ConversionProfiles\0\";
        private const string AutoSavePath = ProfilePath + @"AutoSave\";
        private const string TargetKey = AutoSavePath + "TargetDirectory";

        public static IEnumerable<TestCaseData> Paths()
        {
            yield return new TestCaseData(@"C:\\Temp", @"C:\Temp");
            yield return new TestCaseData(@"C:\Temp", @"C:\Temp");
            yield return new TestCaseData(@"D:\PowerHaus\Dokumente", @"D:\PowerHaus\Dokumente");
            yield return new TestCaseData(@"\\server\share\folder", @"\\server\share\folder");
            yield return new TestCaseData(@"\\\\server\\share\\folder", @"\\server\share\folder");
            yield return new TestCaseData(@"\\pwrhaus\haufe\pwrhaus\dok", @"\\pwrhaus\haufe\pwrhaus\dok");
            yield return new TestCaseData(@"\\pwrhaus\temp\dir", @"\\pwrhaus\temp\dir");
            yield return new TestCaseData(@"\\\\pwrhaus\\temp\\dir", @"\\pwrhaus\temp\dir");
            yield return new TestCaseData(@"C:\new\test", @"C:\new\test");
            yield return new TestCaseData(@"C:\\new\\test", @"C:\new\test");
            yield return new TestCaseData(@"\\server\new\test", @"\\server\new\test");
            yield return new TestCaseData(@"\\\\server\\new\\test", @"\\server\new\test");
            yield return new TestCaseData(@"C:\", @"C:\");
            yield return new TestCaseData(@"C:\\", @"C:\");
            yield return new TestCaseData(@"c:\temp\", @"c:\temp\");
            yield return new TestCaseData(@"\\server\share", @"\\server\share");
            yield return new TestCaseData(@"\\server\share\", @"\\server\share\");
            yield return new TestCaseData(@"C:\Büro & Belege\<DateTime:yyyy-MM>", @"C:\Büro & Belege\<DateTime:yyyy-MM>");
            yield return new TestCaseData("", "");
            // Existing serialized relative/root-relative paths and tokens still decode.
            yield return new TestCaseData(@"reports\\daily", @"reports\daily");
            yield return new TestCaseData(@"\\reports\\daily", @"\reports\daily");
            yield return new TestCaseData(@"\\reports", @"\reports");
            yield return new TestCaseData(@"<MyDocuments>\\reports", @"<MyDocuments>\reports");
            yield return new TestCaseData(@"C:/Temp", @"C:/Temp");
        }

        private static Data ProfileData(string stored)
        {
            var data = Data.CreateDataStorage();
            data.SetValue(@"ConversionProfiles\numClasses", "1");
            data.SetValue(ProfilePath + "Guid", "DefaultGuid");
            data.SetValue(AutoSavePath + "Enabled", "True");
            data.SetValue(AutoSavePath + "EnsureUniqueFilenames", "False");
            data.SetValue(TargetKey, stored);
            return data;
        }

        private static void CheckProfile(clawPDFSettings settings, string expected)
        {
            Assert.That(settings.ConversionProfiles.Count, Is.EqualTo(1));
            var profile = settings.ConversionProfiles[0];
            Assert.That(profile.Guid, Is.EqualTo("DefaultGuid"));
            Assert.That(profile.AutoSave.Enabled, Is.True);
            Assert.That(profile.AutoSave.EnsureUniqueFilenames, Is.False);
            Assert.That(profile.AutoSave.TargetDirectory, Is.EqualTo(expected));
        }

        [TestCaseSource("Paths")]
        public void ReadValues_PreservesWindowsPath(string stored, string expected)
        {
            var settings = new clawPDFSettings(null);
            settings.ReadValues(ProfileData(stored), "");
            CheckProfile(settings, expected);
        }

        [TestCaseSource("Paths")]
        public void StoreAndReload_UsesExistingEscaping(string stored, string expected)
        {
            var settings = new clawPDFSettings(null);
            settings.ReadValues(ProfileData(stored), "");
            var data = Data.CreateDataStorage();
            settings.StoreValues(data, "");
            Assert.That(data.GetValue(TargetKey), Is.EqualTo(Data.EscapeString(expected)));
            var reloaded = new clawPDFSettings(null);
            reloaded.ReadValues(data, "");
            CheckProfile(reloaded, expected);
        }

        [TestCaseSource("Paths")]
        public void Ini_LoadSaveReload_PreservesWindowsPath(string stored, string expected)
        {
            var filename = Path.GetTempFileName();
            try
            {
                // A literal fixture avoids using the writer to manufacture the input.
                File.WriteAllText(filename,
                    "[ConversionProfiles]\nnumClasses=1\n" +
                    "[ConversionProfiles\\0]\nGuid=DefaultGuid\n" +
                    "[ConversionProfiles\\0\\AutoSave]\nEnabled=True\nEnsureUniqueFilenames=False\nTargetDirectory=" + stored + "\n",
                    Encoding.UTF8);
                var settings = new clawPDFSettings(new IniStorage(Encoding.UTF8));
                Assert.That(settings.LoadData(filename), Is.True);
                CheckProfile(settings, expected);
                Assert.That(settings.SaveData(filename), Is.True);
                Assert.That(File.ReadAllText(filename), Does.Contain("TargetDirectory=" + Data.EscapeString(expected)));
                var reloaded = new clawPDFSettings(new IniStorage(Encoding.UTF8));
                Assert.That(reloaded.LoadData(filename), Is.True);
                CheckProfile(reloaded, expected);
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [Test]
        public void MissingTargetDirectory_RemainsEmpty()
        {
            var autoSave = new AutoSave { TargetDirectory = @"C:\Previous" };
            autoSave.ReadValues(Data.CreateDataStorage(), AutoSavePath);
            Assert.That(autoSave.TargetDirectory, Is.Empty);
        }

        [Test]
        public void OtherSettings_StillDecodeEscapes()
        {
            var data = ProfileData(@"C:\new\test");
            data.SetValue(ProfilePath + @"EmailClient\Content", @"line1\r\nline2\tend\\literal\0");
            var profile = new ConversionProfile();
            profile.ReadValues(data, ProfilePath);
            Assert.That(profile.EmailClient.Content, Is.EqualTo("line1\r\nline2\tend\\literal\0"));
            Assert.That(profile.AutoSave.TargetDirectory, Is.EqualTo(@"C:\new\test"));
        }

        [Test]
        public void LegacyDecoder_ReproducesOriginalFailure()
        {
            Assert.Catch<ArgumentException>(() => Data.UnescapeString(@"C:\Temp"));
            Assert.Catch<ArgumentException>(() => Data.UnescapeString(@"\\pwrhaus\haufe\pwrhaus\dok"));
            Assert.That(Data.UnescapeString(@"C:\new\test"), Is.EqualTo("C:\new\test"));
        }

        [TestCase(@"line1\r\nline2\0", "line1\r\nline2\0")]
        [TestCase(@"line1\tline2", "line1\tline2")]
        public void NonPathTargetDirectory_KeepsLegacyEscapeRules(string stored, string expected)
        {
            var autoSave = new AutoSave();
            autoSave.ReadValues(ProfileData(stored), AutoSavePath);
            Assert.That(autoSave.TargetDirectory, Is.EqualTo(expected));
        }

        [Test]
        public void WindowsOutputPath_ContainsUncDirectoryAndDocumentName()
        {
#if NETFRAMEWORK
            var autoSave = new AutoSave();
            autoSave.ReadValues(ProfileData(@"\\pwrhaus\haufe\pwrhaus\dok"), AutoSavePath);
            Assert.That(Path.Combine(autoSave.TargetDirectory, "260925_AS_Journal_111930.pdf"),
                Is.EqualTo(@"\\pwrhaus\haufe\pwrhaus\dok\260925_AS_Journal_111930.pdf"));
#else
            Assert.Ignore("Requires Windows path semantics; run the net462 target.");
#endif
        }

        // The shipped .NET Framework RegistryStorage references Registry.DynData,
        // which is absent in modern .NET. Exercise it on its actual target runtime.
        [TestCaseSource("Paths")]
        public void Registry_LoadSaveReload_PreservesWindowsPath(string stored, string expected)
        {
#if NETFRAMEWORK
            var keyPath = @"Software\clawPDF.Tests\" + Guid.NewGuid().ToString("N");
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath + @"\ConversionProfiles"))
                    key.SetValue("numClasses", "1");
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath + @"\ConversionProfiles\0"))
                    key.SetValue("Guid", "DefaultGuid");
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath + @"\ConversionProfiles\0\AutoSave"))
                {
                    key.SetValue("Enabled", "True");
                    key.SetValue("EnsureUniqueFilenames", "False");
                    key.SetValue("TargetDirectory", stored, RegistryValueKind.String);
                }
                var storage = new RegistryStorage(RegistryHive.CurrentUser, keyPath, true);
                var settings = new clawPDFSettings(storage);
                Assert.That(settings.LoadData(""), Is.True);
                CheckProfile(settings, expected);
                Assert.That(settings.SaveData(""), Is.True);
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath + @"\ConversionProfiles\0\AutoSave"))
                    Assert.That(key.GetValue("TargetDirectory"), Is.EqualTo(Data.EscapeString(expected)));
                var reloaded = new clawPDFSettings(new RegistryStorage(RegistryHive.CurrentUser, keyPath));
                Assert.That(reloaded.LoadData(""), Is.True);
                CheckProfile(reloaded, expected);
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
            }
#else
            Assert.Ignore("Requires Windows and the .NET Framework target (net462).");
#endif
        }
    }
}
