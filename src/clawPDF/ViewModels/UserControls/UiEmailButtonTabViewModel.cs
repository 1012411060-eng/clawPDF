using System.Collections.Generic;
using clawSoft.clawPDF.Core.Settings;
using clawSoft.clawPDF.Core.Settings.Enums;
using clawSoft.clawPDF.Shared.Helper;

namespace clawSoft.clawPDF.ViewModels.UserControls
{
    internal class UiEmailButtonTabViewModel : ApplicationSettingsViewModel
    {
        public UiEmailButtonTabViewModel()
        {
            Tokens = new List<string>(TokenHelper.GetTokenListWithFormatting());
        }

        public List<string> Tokens { get; }

        public UiEmailButtonSettings UiEmailButtonSettings
        {
            get
            {
                if (ApplicationSettings.UiEmailButton == null)
                    ApplicationSettings.UiEmailButton = new UiEmailButtonSettings();

                return ApplicationSettings.UiEmailButton;
            }
            set
            {
                ApplicationSettings.UiEmailButton = value;
                RaisePropertyChanged(nameof(UiEmailButtonSettings));
                RaiseCommandModeChanged();
            }
        }

        public bool IsDefaultMode
        {
            get => UiEmailButtonSettings.Mode == UiEmailButtonMode.Default;
            set
            {
                if (value)
                {
                    UiEmailButtonSettings.Mode = UiEmailButtonMode.Default;
                    RaiseCommandModeChanged();
                }
            }
        }

        public bool IsCommandMode
        {
            get => UiEmailButtonSettings.Mode == UiEmailButtonMode.Command;
            set
            {
                if (value)
                {
                    UiEmailButtonSettings.Mode = UiEmailButtonMode.Command;
                    RaiseCommandModeChanged();
                }
            }
        }

        public bool CommandSectionVisible => UiEmailButtonSettings.Mode == UiEmailButtonMode.Command;

        protected override void OnSettingsChanged(System.EventArgs e)
        {
            base.OnSettingsChanged(e);
            RaisePropertyChanged(nameof(UiEmailButtonSettings));
            RaiseCommandModeChanged();
        }

        private void RaiseCommandModeChanged()
        {
            RaisePropertyChanged(nameof(IsDefaultMode));
            RaisePropertyChanged(nameof(IsCommandMode));
            RaisePropertyChanged(nameof(CommandSectionVisible));
        }
    }
}
